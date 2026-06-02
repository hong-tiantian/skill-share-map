// Leaflet Map Integration for Blazor
// Support multiple map instances

window.mapHelper = {
    maps: {},

    initMap: function (mapId, lat, lng, zoom, dotNetHelper) {
        if (this.maps[mapId]) {
            if (this.maps[mapId].map) this.maps[mapId].map.remove();
            if (this.maps[mapId].dotNetHelper) this.maps[mapId].dotNetHelper.dispose();
        }

        const map = L.map(mapId).setView([lat, lng], zoom);

        const tileLayer = L.tileLayer('https://{s}.basemaps.cartocdn.com/rastertiles/light_all/{z}/{x}/{y}{r}.png', {
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors &copy; <a href="https://carto.com/attributions">CARTO</a>',
            subdomains: 'abcd',
            maxZoom: 20
        });
        tileLayer.addTo(map);

        tileLayer.on('tileerror', (errorEvent) => {
            console.warn('Map tile failed to load.', errorEvent);
        });

        setTimeout(() => { map.invalidateSize(); }, 150);

        const markerLayerGroup = L.layerGroup().addTo(map);

        this.maps[mapId] = {
            map: map,
            markerLayer: markerLayerGroup,
            dotNetHelper: dotNetHelper,
            markers: {}
        };

        return true;
    },

    // Clean Solar-style line icons (mirror of SsmIcons.cs) — monochrome by default;
    // the category color only appears on the hover "glass" state (see site.css).
    categoryIcons: {
        StudyHelp:      '<path d="M12 6.2C10.3 4.9 7.6 4.4 4.5 5v12.3c3.1-.6 5.8-.1 7.5 1.2 1.7-1.3 4.4-1.8 7.5-1.2V5c-3.1-.6-5.8-.1-7.5 1.2Z" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linejoin="round"/><path d="M12 6.2v12.3" stroke="currentColor" stroke-width="1.7" stroke-linecap="round"/>',
        TechHelp:       '<path d="M9.2 8.2 5 12l4.2 3.8M14.8 8.2 19 12l-4.2 3.8" fill="none" stroke="currentColor" stroke-width="1.9" stroke-linecap="round" stroke-linejoin="round"/>',
        CreativeDesign: '<path d="M12 3.5C7.3 3.5 3.5 7 3.5 11.4c0 3.4 2.5 5.1 5.2 5.1 1.2 0 1.9-.8 1.9-1.8 0-.5-.2-.8-.2-1.2 0-.8.6-1.4 1.5-1.4H14c2.6 0 4.5-1.9 4.5-4.8C18.5 6.4 15.7 3.5 12 3.5Z" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linejoin="round"/><circle cx="7.6" cy="11" r="1.05" fill="currentColor"/><circle cx="10.4" cy="7.6" r="1.05" fill="currentColor"/><circle cx="14.4" cy="7.8" r="1.05" fill="currentColor"/>',
        PhotoVideo:     '<rect x="3" y="7" width="18" height="13" rx="3" fill="none" stroke="currentColor" stroke-width="1.7"/><path d="M8.2 7 9.6 4.6h4.8L15.8 7" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linejoin="round"/><circle cx="12" cy="13.4" r="3.2" fill="none" stroke="currentColor" stroke-width="1.7"/>',
        WritingEditing: '<path d="M5 19.2 6 15.6 15.6 6a1.9 1.9 0 0 1 2.7 2.7L8.7 18.3 5 19.2Z" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linejoin="round"/><path d="M14.2 7.4 16.9 10" stroke="currentColor" stroke-width="1.7"/>',
        LanguageHelp:   '<path d="M5 4.5h10a2 2 0 0 1 2 2V12a2 2 0 0 1-2 2H9.5L6 16.8V14H5a2 2 0 0 1-2-2V6.5a2 2 0 0 1 2-2Z" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linejoin="round"/><path d="M6.5 8.2h7M6.5 10.7h4.5" stroke="currentColor" stroke-width="1.5" stroke-linecap="round"/>',
        Job:            '<rect x="3" y="7.5" width="18" height="12" rx="2.5" fill="none" stroke="currentColor" stroke-width="1.7"/><path d="M8.5 7.5V6a2 2 0 0 1 2-2h3a2 2 0 0 1 2 2v1.5" fill="none" stroke="currentColor" stroke-width="1.7"/><path d="M3 12.2h18" stroke="currentColor" stroke-width="1.7"/>',
        Help:           '<circle cx="12" cy="12" r="8.5" fill="none" stroke="currentColor" stroke-width="1.7"/><path d="M9.6 9.5a2.4 2.4 0 0 1 4.7.6c0 1.6-2.3 2-2.3 3.4M12 16.3h.01" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round"/>'
    },

    // Category accent colors (used only for the hover glass + glow). Mirrors the home palette.
    categoryColor: {
        StudyHelp:      '#4a75b0',
        TechHelp:       '#7c3aed',
        CreativeDesign: '#ec4899',
        PhotoVideo:     '#f59e0b',
        WritingEditing: '#16a34a',
        LanguageHelp:   '#0891b2'
    },

    getMapInstance: function (mapId) {
        if (mapId && this.maps[mapId]) return this.maps[mapId];
        if (this.maps['mainMap']) return this.maps['mainMap'];
        const entries = Object.values(this.maps);
        return entries.length > 0 ? entries[0] : null;
    },

    addMarker: function (mapId, id, lat, lng, title, type, category, rare, aiPick) {
        const mapInstance = this.getMapInstance(mapId);
        if (!mapInstance) return false;

        // Resolve color + icon: tasks use their skill category, jobs use a briefcase node.
        let color, iconPath;
        if (type === 'job') {
            color = '#EB1D26';
            iconPath = this.categoryIcons.Job;
        } else {
            color = this.categoryColor[category] || '#4a75b0';
            iconPath = this.categoryIcons[category] || this.categoryIcons.Help;
        }
        const iconSvg = `<svg viewBox="0 0 24 24" width="21" height="21" fill="none">${iconPath}</svg>`;

        const rareClass = rare ? ' rare' : '';
        const aiClass = aiPick ? ' ai' : '';
        const aiEmblem = aiPick ? '<span class="ssm-node-ai">AI</span>' : '';
        const nodeHtml = `
            <div class="ssm-node${rareClass}${aiClass}" style="--node-color:${color};" title="${(title || '').replace(/"/g, '&quot;')}">
                <span class="ssm-node-halo"></span>
                <span class="ssm-node-core">${iconSvg}</span>
                ${aiEmblem}
            </div>`;

        const customIcon = L.divIcon({
            className: 'ssm-node-marker',
            html: nodeHtml,
            iconSize: [42, 42],
            iconAnchor: [21, 21],
            popupAnchor: [0, -22]
        });

        const marker = L.marker([lat, lng], { icon: customIcon });
        mapInstance.markerLayer.addLayer(marker);
        mapInstance.markers[id] = marker;

        marker.on('click', (e) => {
            // Brief "light-up" pulse on the clicked node for tactile feedback.
            const coreEl = marker.getElement() && marker.getElement().querySelector('.ssm-node-core');
            if (coreEl) {
                coreEl.classList.remove('accepted');
                void coreEl.offsetWidth; // restart animation
                coreEl.classList.add('accepted');
            }

            // Zoom in to the marker on click (smooth animation)
            const currentZoom = mapInstance.map.getZoom();
            const targetZoom = Math.max(currentZoom, 16); // Zoom to at least 16
            mapInstance.map.setView(e.latlng, targetZoom, { animate: true, duration: 0.5 });

            // After the zoom/pan completes, get the screen-relative position
            // Use a short delay to let the animation settle
            setTimeout(() => {
                if (mapInstance.dotNetHelper) {
                    // Get the map container's position on screen
                    const containerEl = mapInstance.map.getContainer();
                    const containerRect = containerEl.getBoundingClientRect();
                    
                    // Get the marker's pixel position relative to the container
                    const point = mapInstance.map.latLngToContainerPoint(e.latlng);
                    
                    // Convert to screen-relative coords
                    const screenX = containerRect.left + point.x;
                    const screenY = containerRect.top + point.y;

                    mapInstance.dotNetHelper.invokeMethodAsync('HandleMarkerClick', id, screenX, screenY);
                }
            }, 550); // Wait for zoom animation to finish
        });

        return true;
    },

    clearMarkers: function (mapId) {
        const mapInstance = this.getMapInstance(mapId);
        if (!mapInstance || !mapInstance.markerLayer) return false;
        mapInstance.markerLayer.clearLayers();
        mapInstance.markers = {};
        return true;
    },

    // Trigger the "light-up" burst on a specific node (e.g. when a task is accepted).
    lightUpMarker: function (mapId, id) {
        const mapInstance = this.getMapInstance(mapId);
        if (!mapInstance || !mapInstance.markers) return false;
        const marker = mapInstance.markers[id];
        if (!marker || !marker.getElement) return false;
        const coreEl = marker.getElement().querySelector('.ssm-node-core');
        if (coreEl) {
            coreEl.classList.remove('accepted');
            void coreEl.offsetWidth;
            coreEl.classList.add('accepted');
        }
        return true;
    },

    // Sweep a glowing "AI scanning" overlay across the map (used when the
    // intelligence layer surfaces picks). Self-removes after the animation.
    scanSweep: function (mapId) {
        const mapInstance = this.getMapInstance(mapId);
        if (!mapInstance || !mapInstance.map) return false;
        const container = mapInstance.map.getContainer();
        const existing = container.querySelector('.ssm-map-scan');
        if (existing) existing.remove();
        const el = document.createElement('div');
        el.className = 'ssm-map-scan';
        container.appendChild(el);
        setTimeout(() => { if (el && el.parentNode) el.parentNode.removeChild(el); }, 1700);
        return true;
    },

    fitBounds: function (mapId) {
        const mapInstance = this.getMapInstance(mapId);
        if (!mapInstance || !mapInstance.markerLayer) return false;
        const layers = mapInstance.markerLayer.getLayers();
        if (layers.length > 0) {
            const group = L.featureGroup(layers);
            mapInstance.map.fitBounds(group.getBounds().pad(0.1));
        }
        mapInstance.map.invalidateSize();
        return true;
    },

    setView: function (mapId, lat, lng, zoom) {
        const mapInstance = this.getMapInstance(mapId);
        if (mapInstance && mapInstance.map) {
            mapInstance.map.setView([lat, lng], zoom, { animate: true });
        }
        return true;
    },

    getCenter: function (mapId) {
        const mapInstance = this.getMapInstance(mapId);
        if (mapInstance && mapInstance.map) {
            const center = mapInstance.map.getCenter();
            return { lat: center.lat, lng: center.lng };
        }
        return null;
    }
};
