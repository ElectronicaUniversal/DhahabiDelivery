// Leaflet Map Interop - Wrapper para controlar Leaflet desde C#
// Mismo enfoque (Leaflet + tiles OpenStreetMap/CARTO) usado en BusinessPlaceClient,
// extendido para soportar varios marcadores independientes por clave (ej. destino + posición del repartidor).
let map = null;
let markers = {};
let clickCallback = null;

export async function initializeMap(containerId, lat, lng, zoom) {
    try {
        if (typeof L === "undefined") {
            console.error("Leaflet no está cargado. Asegúrate de incluir el CDN en el HTML.");
            return false;
        }

        if (map) {
            map.remove();
            map = null;
            markers = {};
        }

        map = L.map(containerId, {
            center: [lat, lng],
            zoom: zoom,
            zoomControl: false,
            attributionControl: true
        });

        L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
            attribution: "&copy; <a href=\"https://www.openstreetmap.org/copyright\">OpenStreetMap</a> contributors",
            maxZoom: 19
        }).addTo(map);

        setTimeout(() => {
            if (map) map.invalidateSize();
        }, 100);

        return true;
    } catch (error) {
        console.error("Error inicializando el mapa:", error);
        return false;
    }
}

export function addMarker(key, lat, lng, customIconHtml = null) {
    if (!map) {
        console.error("El mapa no está inicializado");
        return;
    }

    if (markers[key]) {
        map.removeLayer(markers[key]);
    }

    let markerOptions = {};
    if (customIconHtml) {
        markerOptions.icon = L.divIcon({
            html: customIconHtml,
            className: "custom-leaflet-marker",
            iconSize: [40, 40],
            iconAnchor: [20, 40]
        });
    }

    markers[key] = L.marker([lat, lng], markerOptions).addTo(map);
}

export function removeMarker(key) {
    if (map && markers[key]) {
        map.removeLayer(markers[key]);
        delete markers[key];
    }
}

export function setCenter(lat, lng, zoom = null) {
    if (!map) {
        console.error("El mapa no está inicializado");
        return;
    }

    if (zoom !== null) {
        map.setView([lat, lng], zoom);
    } else {
        map.panTo([lat, lng]);
    }
}

export function onMapClick(dotnetReference, methodName) {
    if (!map) {
        console.error("El mapa no está inicializado");
        return;
    }

    if (clickCallback) {
        map.off("click", clickCallback);
    }

    clickCallback = (e) => {
        dotnetReference.invokeMethodAsync(methodName, e.latlng.lat, e.latlng.lng);
    };

    map.on("click", clickCallback);
}

export function invalidateSize() {
    if (map) map.invalidateSize();
}

export function dispose() {
    if (map) {
        map.remove();
        map = null;
        markers = {};
        clickCallback = null;
    }
}
