using _Microsoft.Android.Resource.Designer;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Media;
using Bumptech.Glide;
using Bumptech.Glide.Request.Target;
using Bumptech.Glide.Request.Transition;
using ME.Pushy.Sdk;
using Object = Java.Lang.Object;
using Uri = Android.Net.Uri;


namespace DhahabiDelivery;

[BroadcastReceiver(Enabled = true, Exported = false)]
[IntentFilter(["pushy.me"])]
public class PushReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (intent == null || context == null) return;

        var notificationData = GetNotificationData(intent);


        const int smallIcon = ResourceConstant.Drawable.abc_ab_share_pack_mtrl_alpha;

        Task.Run(async () =>
        {
            var builder = new Notification.Builder(context)
                .SetAutoCancel(true)
                .SetSmallIcon(smallIcon)
                .SetContentTitle(notificationData.Title)
                .SetContentText(notificationData.Message)
                .SetSound(RingtoneManager.GetDefaultUri(RingtoneType.Notification))
                ?.SetPendingIntent(context, notificationData.Url);

            switch (notificationData.Style)
            {
                case NotificationConstants.BigPictureStyle:
                    var bigPictureStyle =
                        await new Notification.BigPictureStyle().SetBigPicture(context, notificationData.BigPicture);
                    builder = builder?.SetStyle(bigPictureStyle);
                    break;
                case NotificationConstants.BigTextStyle:
                    var bigTextStyle = new Notification.BigTextStyle().BigText(notificationData.BigText);
                    builder = builder?.SetStyle(bigTextStyle);
                    break;
            }

            if (builder == null) return;
            builder = await builder.SetLargeIconAsync(context, notificationData.LargeIcon);

            // Automatically configure a Notification Channel for devices running And   roid O+
            Pushy.SetNotificationChannel(builder, context);

            // Get an instance of the NotificationManager service Build the notification and display it 
            var notificationManager = (NotificationManager?)context.GetSystemService(Context.NotificationService);
            notificationManager?.Notify(notificationData.Id, builder?.Build());
        });
    }

    private static NotificationData GetNotificationData(Intent? intent)
    {
        return new NotificationData
        {
            Id = intent?.GetIntExtra("id", new Random().Next()) ?? new Random().Next(),
            Title = intent?.GetStringExtra("title") ?? string.Empty,
            Message = intent?.GetStringExtra("message") ?? string.Empty,
            LargeIcon = intent?.GetStringExtra("largeIcon") ?? string.Empty,
            Url = intent?.GetStringExtra("url") ?? string.Empty,
            Style = intent?.GetStringExtra("style") ?? string.Empty,
            BigPicture = intent?.GetStringExtra("bigPicture") ?? string.Empty,
            BigText = intent?.GetStringExtra("bigText") ?? string.Empty
        };
    }
}

internal class NotificationData
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string LargeIcon { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Style { get; set; } = string.Empty;
    public string BigPicture { get; set; } = string.Empty;
    public string BigText { get; set; } = string.Empty;
    public int Id { get; set; }
}

public delegate void CallbackDelegate(Bitmap? drawable);

internal class BitmapTarget(CallbackDelegate callback) : CustomTarget
{
    public override void OnResourceReady(Object resource, ITransition transition)
    {
        callback((Bitmap?)resource);
    }

    public override void OnLoadCleared(Drawable p0)
    {
        callback(null);
    }

    public override void OnLoadFailed(Drawable p0)
    {
        callback(null);
    }
}

internal static class NotificationBuilderExtensions
{
    public static Notification.Builder SetPendingIntent(this Notification.Builder builder, Context context, string url)
    {
        if (string.IsNullOrEmpty(url)) return builder;
        var intent = new Intent(context, typeof(MainActivity));
        var uri = Uri.Parse(url);
        intent.SetData(uri);
        intent.SetAction(Intent.ActionView);
        var pendingIntent = PendingIntent.GetActivity(context, 0, intent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
        builder.SetContentIntent(pendingIntent);
        return builder;
    }

    public static async Task<Notification.Builder> SetLargeIconAsync(this Notification.Builder builder,
        Context context, string url)
    {
        if (string.IsNullOrEmpty(url)) return builder;
        var taskCompletionSource = new TaskCompletionSource<Bitmap?>();
        var customTarget = new BitmapTarget(taskCompletionSource.SetResult);
        Glide.With(context).AsBitmap().Load(url).Into(customTarget);
        var bitmap = await taskCompletionSource.Task;
        builder.SetLargeIcon(bitmap);
        return builder;
    }

    public static async Task<Notification.BigPictureStyle> SetBigPicture(
        this Notification.BigPictureStyle builder,
        Context context, string url)
    {
        if (string.IsNullOrEmpty(url)) return builder;
        var taskCompletionSource = new TaskCompletionSource<Bitmap?>();
        var customTarget = new BitmapTarget(taskCompletionSource.SetResult);
        Glide.With(context).AsBitmap().Load(url).Into(customTarget);
        var bitmap = await taskCompletionSource.Task;
        builder.BigPicture(bitmap);
        return builder;
    }
}

internal class NotificationConstants
{
    public const string BigPictureStyle = "BigPictureStyle";
    public const string BigTextStyle = "BigTextStyle";
}