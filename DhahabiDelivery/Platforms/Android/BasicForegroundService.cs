using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using Application = Android.App.Application;

namespace DhahabiDelivery;

[Service(
    ForegroundServiceType = ForegroundService.TypeDataSync, Exported = false)]
public class BasicForegroundService : Service
{
    private const int ServiceId = 1002;
    private const string ChannelId = "delivery_channel";
    private const string ChannelName = "Servicio de Ubicación";
    private const string ChannelDescription = "Notificaciones del servicio de ubicación para entregas";

    public override IBinder? OnBind(Intent? intent)
    {
        return null;
    }

    public override void OnCreate()
    {
        base.OnCreate();
        CreateNotificationChannel();
    }

    private static void CreateNotificationChannel()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O) return;
        var channel = new NotificationChannel(ChannelId, ChannelName, NotificationImportance.High);
        channel.Description = ChannelDescription;

        channel.SetShowBadge(false);
        channel.EnableLights(true);
        channel.EnableVibration(true);

        var notificationManager = Application.Context.GetSystemService(NotificationService) as NotificationManager;
        notificationManager?.CreateNotificationChannel(channel);
    }

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        try
        {
            // Crear notificación persistente mejorada
            var notification = CreateNotification();
            StartForeground(ServiceId, notification);
        }
        catch (Exception secEx)
        {
            Console.WriteLine($"error al crear el servicio: {secEx}");
        }

        return StartCommandResult.Sticky;
    }

    private Notification CreateNotification()
    {
        var builder = new NotificationCompat.Builder(this, ChannelId)
            .SetContentTitle("titulo de ejemplo")
            .SetContentText("Esto es una notification bastante simple")
            .SetSmallIcon(Android.Resource.Drawable.IcDialogInfo)
            .SetOngoing(true)
            .SetAutoCancel(false)
            .SetForegroundServiceBehavior(NotificationCompat.ForegroundServiceImmediate)
            .SetCategory(NotificationCompat.CategoryService)
            .SetVisibility(NotificationCompat.VisibilityPublic)
            .SetPriority(NotificationCompat.PriorityHigh);
        return builder.Build();
    }
}