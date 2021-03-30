(function (window) {
    'use strict';

    function define_GeoWebGIS() {
        var GeoWebGIS = {
            Options: {
                Bounds: [609681, 267328, 610178, 267775],
                Extent: [420000, 30000, 900000, 350000],
                Resolutions: [4000, 3750, 3500, 3250, 3000, 2750, 2500, 2250, 2000, 1750, 1500, 1250, 1000, 750, 650, 500, 250, 100, 50, 20, 10, 5, 2.5, 2, 1.5, 1, 0.5, 0.25, 0.1],
                Center: [600000, 200000],
                isInternational: false
            },
            mapguidelayer: null,
            drawlayer: null,
            geojsonlayerCluster: null,
            projection: null,
            map: null,
            matrixIds: [],
            attributions: null,
            applicationdefinition: null,
            satmap: null,
            backgroudmap: null,
            osmBackgroundmap: null,
            satellitemap: null,
            bingmap: null,
            geolocation: null,
            positionFeature: null,
            positionLayer: null,
            geojsonlayer: null,
            geologielayer: null,
            wfslayer: null,
            wfssource: null,
            customLayers: [],
            loadstate: 1,
            draw_interaction: null,
            clusterselect: null,
            showLabels: false,
            isDrawing: false, 
            workspace: '',
            selectedfeature: null
        };

        GeoWebGIS.initialize = function (options) {

            proj4.defs("EPSG:21781", "+proj=somerc +lat_0=46.95240555555556 +lon_0=7.439583333333333 +k_0=1 +x_0=600000 +y_0=200000 +ellps=bessel +towgs84=674.4,15.1,405.3,0,0,0,0 +units=m +no_defs");
            ol.proj.proj4.register(proj4);
            options = options || {};

            for (var i = 0; i < GeoWebGIS.Options.Resolutions.length; i++) {
                this.matrixIds.push(i);
            }

        }

        /**
         * Function: createMap
         *
         * Function that creates the map
         *
         * Return: 
         * nothing
         */

        GeoWebGIS.createMap = function (islogin) {
            var self = this;


            this.projection = ol.proj.get('EPSG:21781');
            this.projection.setExtent(GeoWebGIS.Options.Extent);


            var mapproxyurl = '/MapImageProxy/{Layer}/{TileMatrix}/{TileCol}/{TileRow}';

            this.backgroudmap_old = new ol.layer.Tile({
                source: new ol.source.WMTS(({
                    layer: 'swisstopo-pixelkarte',
                    crossOrigin: 'anonymous',
                    attributions: this.attributions,
                    url: $(location).attr('protocol') + '//' + $(location).attr('host') + mapproxyurl,
                    tileGrid: new ol.tilegrid.WMTS({
                        origin: [GeoWebGIS.Options.Extent[0], GeoWebGIS.Options.Extent[3]],
                        resolutions: GeoWebGIS.Options.Resolutions,
                        matrixIds: this.matrixIds
                    }),
                    requestEncoding: 'REST'
                }))
            });

            this.backgroudmap = new ol.layer.Tile({
                source: new ol.source.WMTS(({
                    layer: 'ch.swisstopo.pixelkarte-farbe',
                    crossOrigin: 'anonymous',
                    attributions: this.attributions,
                    url: 'https://wmts100.geo.admin.ch/1.0.0/ch.swisstopo.pixelkarte-farbe/default/current/2056/{TileMatrix}/{TileCol}/{TileRow}.jpeg',
                    tileGrid: new ol.tilegrid.WMTS({
                        origin: [GeoWebGIS.Options.Extent[0], GeoWebGIS.Options.Extent[3]],
                        resolutions: GeoWebGIS.Options.Resolutions,
                        matrixIds: this.matrixIds
                    }),
                    requestEncoding: 'REST'
                }))
            });

            this.satellitemap = new ol.layer.Tile({
                source: new ol.source.WMTS(({
                    layer: 'ch.swisstopo.pixelkarte-farbe',
                    crossOrigin: 'anonymous',
                    attributions: this.attributions,
                    url: 'https://wmts100.geo.admin.ch/1.0.0/ch.swisstopo.swissimage/default/current/2056/{TileMatrix}/{TileCol}/{TileRow}.jpeg',
                    tileGrid: new ol.tilegrid.WMTS({
                        origin: [GeoWebGIS.Options.Extent[0], GeoWebGIS.Options.Extent[3]],
                        resolutions: GeoWebGIS.Options.Resolutions,
                        matrixIds: this.matrixIds
                    }),
                    requestEncoding: 'REST'
                }))
            });


            this.osmBackgroundmap = new ol.layer.Tile({
                source: new ol.source.OSM()
            });

            var gj;

            var getText = function (feature) {
                var text = feature.get('name');
                return text;
            };

            var createTextStyle = function (feature) {
                var offsetX = 0;
                var offsetY = 0;

                if (feature.getGeometry().getType() == 'Point') {
                    offsetX = 14;
                    offsetY = -14;
                }


                return new ol.style.Text({
                    font: '16px Calibri,sans-serif',
                    align: 'Start',
                    offsetX: offsetX,
                    offsetY: offsetY,
                    text: getText(feature),
                    fill: new ol.style.Fill({
                        color: '#000'
                    }),
                    stroke: new ol.style.Stroke({
                        color: '#fff',
                        width: 8
                    })
                });
            };


            function getColor(feature) {
                return 'blue';
            }

            function polygonStyleFunction(feature) {
                return new ol.style.Style({
                    stroke: new ol.style.Stroke({
                        color: getColor(feature),
                        width: 5
                    }),
                    fill: new ol.style.Fill({
                        color: 'rgba(0,0,255, 0.1)'
                    }),
                    text: createTextStyle(feature)
                });
            }

            function lineStyleFunction(feature) {
                return new ol.style.Style({
                    stroke: new ol.style.Stroke({
                        color: getColor(feature),
                        width: 5
                    }),
                    fill: new ol.style.Fill({
                        color: 'rgba(0,0,255, 0.1)'
                    }),
                    text: createTextStyle(feature)
                });
            }

            function pointStyleFunction(feature) {
                return new ol.style.Style({
                    image: new ol.style.Circle({
                        radius: 5,
                        fill: new ol.style.Fill({
                            color: 'rgba(0,0,255, 0.1)'
                        }),
                        stroke: new ol.style.Stroke({ color: getColor(feature), width: 3 })

                    }),
                    text: createTextStyle(feature)
                });
            }

            var styleFunction = function (feature) {
                if (feature.getGeometry().getType() == 'Point') return pointStyleFunction(feature);
                if (feature.getGeometry().getType() == 'LineString') return lineStyleFunction(feature);
                if (feature.getGeometry().getType() == 'Polygon') return polygonStyleFunction(feature);
            };



            if (!islogin) {
                var gj = new ol.source.Vector({
                    url: '/ReferenceGeometry/GetUserJson',
                    'projection': this.projection,
                    format: new ol.format.GeoJSON({ featureProjection: 'EPSG:21781', dataProjection: 'EPSG:21781', })
                });

                this.geojsonlayer = new ol.layer.Vector({
                    title: 'Beobachtungen',
                    id: 'beobachtungen',
                    source: gj,
                    style: styleFunction,
                    zIndex: 100,
                    minResolution: 0,
                    maxResolution: 4000
                });

                var listenerKey = gj.on('change', function (e) {
                    var self = this;
                    if (gj.getState() == 'ready') {
                        try {
                            GeoWebGIS.map.getView().fit(gj.getExtent(), GeoWebGIS.map.getSize());
                            var center = GeoWebGIS.map.getView().getCenter();
                            var resolution = GeoWebGIS.map.getView().getResolution();

                            var menuWidths = $('#olsidebar').width() + $('.page-sidebar').first().offset().left + $('.page-sidebar').first().width();
                            var newCoordinates = [center[0] - ((menuWidths) * resolution), center[1] + 0 * resolution];

                            GeoWebGIS.map.getView().setCenter(newCoordinates);
                            GeoWebGIS.map.getView().setZoom(GeoWebGIS.map.getView().getZoom() - 1);
                            
                        }
                        catch (fehler) {
                            console.error(fehler);
                        }
                        ol.Observable.unByKey(listenerKey);
                        // do we have to load e specific feature?
                        if (GeoWebGIS.selectedfeature != null) {
                            var selsource = GeoWebGIS.geojsonlayer.getSource();
                            var feature = selsource.getFeatureById(GeoWebGIS.selectedfeature)
                            // push the feature
                            GeoWebGIS.clusterselect.getFeatures().push(feature);
                            GeoWebGIS.selectedfeature = null;

                            // zoom to
                            GeoWebGIS.map.getView().fit(feature.getGeometry().getExtent(), GeoWebGIS.map.getSize());
                            var center = GeoWebGIS.map.getView().getCenter();
                            var resolution = GeoWebGIS.map.getView().getResolution();

                            var menuWidths = $('#olsidebar').width() + $('.page-sidebar').first().offset().left + $('.page-sidebar').first().width();
                            var newCoordinates = [center[0] - ((menuWidths) * resolution), center[1] + 0 * resolution];

                            GeoWebGIS.map.getView().setCenter(newCoordinates);
                            GeoWebGIS.map.getView().setZoom(GeoWebGIS.map.getView().getZoom() - 1);


                        }
                    }
                });

                var myView;

                myView = new ol.View({
                    projection: this.projection,
                    center: this.Options.Center,
                    resolution: 10,
                    minZoom: 3,
                    maxZoom: 16
                })


                // GPS
                this.geolocation = new ol.Geolocation({
                    projection: this.projection
                });

                var gpsstroke = new ol.style.Stroke({ color: 'red', width: 2 });

                var gpsstyle = [
                    new ol.style.Style({ image: new ol.style.RegularShape({ points: 4, radius: 14, radius1: 0, radius2: 0, stroke: gpsstroke }), snapToPixel: true }),
                    new ol.style.Style({ image: new ol.style.Circle({ radius: 10, stroke: gpsstroke }), snapToPixel: true }),
                ];

                this.positionFeature = new ol.Feature();
                this.positionFeature.setStyle(gpsstyle);
                this.geolocation.on('change:position', function () {
                    var self = this;
                    var coordinates = GeoWebGIS.geolocation.getPosition();
                    GeoWebGIS.positionFeature.setGeometry(coordinates ?
                        new ol.geom.Point(coordinates) : null);

                    var resolution = GeoWebGIS.map.getView().getResolution();
                    var menuWidths = $('#olsidebar').width() + $('.page-sidebar').first().offset().left + $('.page-sidebar').first().width();
                    var newCoordinates = [coordinates[0] - ((menuWidths / 2) * resolution), coordinates[1] + 0 * resolution];
                    GeoWebGIS.map.getView().setCenter(newCoordinates);
                });

                this.geolocation.on('error', function (error) {
                    $.notify({
                        title: 'GPS konnte nicht aktiviert werden',
                        message: 'Der Zugriff auf das GPS muss erlaubt sein. Bitte aktivieren (Fehler: ' + error + ')'
                    },
                        {
                            type: 'danger',
                            allow_dismiss: false,
                            newest_on_top: false,
                            mouse_over: false,
                            showProgressbar: false,
                            spacing: 10,
                            timer: 2000,
                            placement: {
                                from: 'top',
                                align: 'right'
                            },
                            offset: {
                                x: 30,
                                y: 100
                            },
                            delay: 1000,
                            z_index: 10000,
                            animate: {
                                enter: 'animated fadeInDown',
                                exit: 'animated bounceOutRight'
                            }
                        });
                });

                this.positionLayer = new ol.layer.Vector({
                    source: new ol.source.Vector({
                        features: [this.positionFeature]
                    })
                });

                this.map = new ol.Map({
                    layers: [this.backgroudmap, this.geojsonlayer, this.positionLayer],
                    target: 'map',

                    view: myView,
                    controls: ol.control.defaults({
                        attributionOptions: ({
                            collapsible: false
                        })
                    }),
                    logo: false
                });

                /*
                this.geolocation = new ol.Geolocation({
                    trackingOptions: {
                        enableHighAccuracy: true,
                    },
                    projection: myView.getProjection(),
                });
                var self = this;
                this.geolocation.on('change:position', function () {
                    var coordinates = self.geolocation.getPosition();
                    self.map.getView().animate({
                        center: coordinates,
                        zoom: Math.max(self.map.getView().getZoom(), 5)
                    });
                });*/






            }
            else {
                this.map = new ol.Map({
                    layers: [this.backgroudmap],
                    target: 'map',

                    view: new ol.View({
                        projection: this.projection,
                        center: this.Options.Center,
                        resolution: 10
                    }),
                    controls: ol.control.defaults({
                        attributionOptions: ({
                            collapsible: false
                        })
                    }),
                    logo: false
                });
            }

            this.clusterselect = new ol.interaction.Select({
                condition: ol.events.condition.click,
                layers: [this.geojsonlayer],
                style: function (feature) {
                    var returnStyle;

                    if (feature.getGeometry().getType() == 'Point') {
                        returnStyle = [new ol.style.Style({
                            image: new ol.style.Circle({
                                radius: 5,
                                fill: new ol.style.Fill({
                                    color: 'rgba(0,255,255, 1)'
                                }),
                                stroke: new ol.style.Stroke({ color: 'rgba(0,255,255, 1)', width: 3 })

                            }),
                            text: createTextStyle(feature)
                        }),
                        new ol.style.Style({
                            image: new ol.style.Circle({
                                radius: 8,
                                stroke: new ol.style.Stroke({ color: 'white', width: 3 })
                            })
                        })
                        ];
                    }
                    else {
                        var normalStyle = styleFunction(feature);
                        normalStyle.getStroke().setColor('rgba(0,255,255,1)');

                        returnStyle = [
                            new ol.style.Style({
                                stroke: new ol.style.Stroke({ color: 'white', width: 8 })
                            }),
                            normalStyle
                        ];
                    }
                    return returnStyle;
                }
            });


            this.map.addInteraction(this.clusterselect);
            this.clusterselect.on('select', function (e) {
                if (e.target.getFeatures().getLength() > 0) {

                    var firstfeature = e.target.getFeatures().getArray();
                    e.selected.forEach(function (feature) {
                        var goToInsideUrl = "/ReferenceGeometry/Details/" + firstfeature[0].getId();
                        loadBeobachtungsURL(goToInsideUrl);
                        sidebar.open("beobachtungen");
                        GeoWebGIS.selectedfeature = firstfeature[0];
                    });

                }
            });


        };

        function getLayer(id) {
            for (var i = 0, numLayers = GeoWebGIS.customLayers.length; i < numLayers; i++) {
                if (GeoWebGIS.customLayers[i].id == id) {
                    return GeoWebGIS.customLayers[i].layer;
                }
            }
        }

        GeoWebGIS.reloadGeoJSON = function () {
            GeoWebGIS.geojsonlayer.getSource().clear();

            var gj = new ol.source.Vector({
                url: '/ReferenceGeometry/GetUserJson',
                'projection': this.projection,
                format: new ol.format.GeoJSON({ featureProjection: 'EPSG:21781', dataProjection: 'EPSG:21781', })
            });
            GeoWebGIS.geojsonlayer.setSource(gj);

            //            GeoWebGIS.geojsonlayer.getSource().addFeatures(format.readFeatures(newjson));
        }

        GeoWebGIS.setOpacity = function (id, opacity) {
            var self = this;
            var la = getLayer(id);
            la.setOpacity(opacity);
        }

        GeoWebGIS.showLayer = function (id) {
            var self = this;
            var la = getLayer(id);
            GeoWebGIS.map.addLayer(getLayer(id));
        }

        GeoWebGIS.hideLayer = function (id) {
            var self = this;
            var la = getLayer(id);
            GeoWebGIS.map.removeLayer(getLayer(id));
        }


        /// Draw interaction
        GeoWebGIS.addDrawInteraction = function (geomType, sqlcolumn, mode, featureId, projectid, groupid) {
            //remove other interactions
            var dirty = {};
            var self = this;
            this.isDrawing = true;
            // clear the map
            this.drawsource = null;
            this.drawlayer = null;

            var modifystyles = [
                new ol.style.Style({
                    stroke: new ol.style.Stroke({
                        color: 'rgba(100,100,100, 0.01)',
                        width: 5
                    }),
                    image: new ol.style.Circle({
                        radius: 5,
                        fill: new ol.style.Fill({
                            color: 'rgba(255,0,0, 0.01)'
                        }),
                        stroke: new ol.style.Stroke({ color: 'rgba(255,0,0, 0.01)', width: 5 })

                    }),
                    fill: new ol.style.Fill({
                        color: 'rgba(255, 255, 255, 0.01)'
                    })
                })];

            GeoWebGIS.drawsource = new ol.source.Vector({
                format: new ol.format.GeoJSON(),
                url: function (extent) {
                    var wfsurl = '/proxy/' + GeoWebGIS.workspace + '/ows?service=WFS&version=2.0.0&request=GetFeature&SRSNAME=EPSG:21781&typeName=' + GeoWebGIS.workspace + ':geometries_' + sqlcolumn + '&outputFormat=application/json&cql_filter=statusid<3 and projectid=\'' + projectid +'\'';
                    return wfsurl;

                },
                strategy: ol.loadingstrategy.bbox
            });



            this.drawlayer = new ol.layer.Vector({
                name: 'drawlayer',
                source: GeoWebGIS.drawsource,
                style: modifystyles,
                zIndex: 101
            });


            //GeoWebGIS.map.removeLayer(self.geojsonlayer);
            GeoWebGIS.map.removeLayer(self.drawlayer);
            GeoWebGIS.map.addLayer(this.drawlayer);



            if (this.clusterselect) {
                this.clusterselect.getFeatures().clear();
            }

            this.map.removeInteraction(this.clusterselect);

            if (mode == "Draw") {

                // create the interaction
                this.draw_interaction = null;
                this.draw_interaction = new ol.interaction.Draw({
                    source: this.drawlayer.getSource(),
                    style: new ol.style.Style({
                        stroke: new ol.style.Stroke({
                            color: 'rgba(255,0,0, 1)',
                            width: 2
                        }),
                        image: new ol.style.Circle({
                            radius: 5,
                            fill: new ol.style.Fill({
                                color: 'rgba(255,0,0, 0.1)'
                            }),
                            stroke: new ol.style.Stroke({ color: 'rgba(255,0,0, 1)', width: 5 })

                        }),
                        fill: new ol.style.Fill({
                            color: 'rgba(255, 255, 255, 0.5)'
                        })
                    }),
                    geometryName: sqlcolumn,
                    type: /** @type {ol.geom.GeometryType} */ geomType
                });


                this.draw_interaction.on('drawstart',
                    function (evt) {
                        // set sketch
                        GeoWebGIS.sketch = evt.feature;

                        /** @type {ol.Coordinate|undefined} */
                        var tooltipCoord = evt.coordinate;

                        GeoWebGIS.listener = GeoWebGIS.sketch.getGeometry().on('change', function (evt) {
                            /*var geom = evt.target;
                            var output;
                            if (geom instanceof ol.geom.Polygon) {
                                output = formatArea(geom);
                                tooltipCoord = geom.getInteriorPoint().getCoordinates();
                            } else if (geom instanceof ol.geom.LineString) {
                                output = formatLength(geom);
                                tooltipCoord = geom.getLastCoordinate();
                            }
                            GeoWebGIS.measureTooltipElement.innerHTML = output;
                            GeoWebGIS.measureTooltip.setPosition(tooltipCoord);*/
                        });
                    }, this);

                // add it to the map
                this.map.addInteraction(this.draw_interaction);
                this.draw_interaction.on('drawend', function (e) {

                    //GeoWebGIS.map.removeOverlay(GeoWebGIS.measureTooltip);
                    ol.Observable.unByKey(GeoWebGIS.listener);


                        if (projectid != 0) {
                            // Parameter setzen
                            e.feature.setProperties({ 'projectid': projectid });
                            e.feature.setProperties({ 'statusid': '1' });
                            e.feature.setProperties({ 'groupid': groupid });
                            var parser = new jsts.io.OL3Parser();

                            // convert the OpenLayers geometry to a JSTS geometry
                            var jstsGeom = parser.read(e.feature.getGeometry());

                            if (jstsGeom.isValid()) {
                                modalokclick = false;

                                $('#newGeometryModal').on('shown.bs.modal', function () {
                                    $('#newGeometryTitle').focus();
                                })

                                $('#newGeometryModal').modal('show');
                                

                                $('#newGeometryTitle').on('keyup keypress', function (e) {
                                    var keyCode = e.keyCode || e.which;
                                    if (keyCode === 13) {
                                        e.preventDefault();
                                        modalokclick = true;
                                        $('#newGeometryModal').modal('hide');
                                    }
                                });

                                var newFeature = e.feature;
                                $('#newGeometryModal').on('hide.bs.modal', function (event) {
                                    if (modalokclick) {
                                        newFeature.setProperties({ 'geometryname': $('#newGeometryTitle').val() });
                                        self.saveData('insert', newFeature);
                                    }
                                    GeoWebGIS.endDrawInteraction()
                                    $(this).off('hide.bs.modal');
                                    $(this).off('shown.bs.modal');
                                })



                            }
                            else {
                                /*BootstrapDialog.show({
                            type: BootstrapDialog.TYPE_DANGER,
                            title: GeoWebGIS.translator.get("wrongPolyTitle"),
                            message: GeoWebGIS.translator.get("wrongPolyText") + '<img src="/Content/themes/invalid_polygon.png" width="560" />',
                            buttons: [{
                                label: 'OK',
                                action: function (dialogItself) {
                                    dialogItself.close();
                                }
                            }]
                        });*/
                                // TODO Fehlermeldung invalid Polygon

                            }

                            //self.map.removeInteraction(self.draw_interaction);
                        }

                });

                var snap = new ol.interaction.Snap({
                    source: this.drawsource
                });
                this.map.addInteraction(snap);
            }
            else if (mode == "Modify") {
                                
                var modifystyles = [
                    /* We are using two different styles for the polygons:
                     *  - The first style is for the polygons themselves.
                     *  - The second style is to draw the vertices of the polygons.
                     *    In a custom `geometry` function the vertices of a polygon are
                     *    returned as `MultiPoint` geometry, which will be used to render
                     *    the style.
                     */
                    new ol.style.Style({
                        stroke: new ol.style.Stroke({
                            color: 'rgba(255,0,0, 1)',
                            width: 5
                        }),
                        image: new ol.style.Circle({
                            radius: 5,
                            fill: new ol.style.Fill({
                                color: 'rgba(255,0,0, 1)'
                            }),

                        }),
                        fill: new ol.style.Fill({
                            color: 'rgba(255, 255, 255, 0.5)'
                        })
                    }),
                    new ol.style.Style({
                        image: new ol.style.Circle({
                            radius: 7,
                            fill: new ol.style.Fill({
                                color: 'red',
                            }),
                        }),
                        geometry: function (feature) {
                            // return the coordinates of the first ring of the polygon
                            const coordinates = sqlcolumn != 'polygon' ? feature.getGeometry().getCoordinates() : feature.getGeometry().getCoordinates()[0];
                            return new ol.geom.MultiPoint(coordinates);
                        },
                    })];

                var interactionSelectPointerMove = new ol.interaction.Select({

                });
                this.interactionSelect = new ol.interaction.Select({
                    condition: ol.events.condition.click,
                    layers: [this.drawlayer],
                    style: function () {
                        if (sqlcolumn == 'point') {
                            return new ol.style.Style({
                                stroke: new ol.style.Stroke({
                                    color: 'rgba(255,0,0, 1)',
                                    width: 2
                                }),
                                image: new ol.style.Circle({
                                    radius: 5,
                                    fill: new ol.style.Fill({
                                        color: 'rgba(255,0,0, 1)'
                                    }),

                                }),
                                fill: new ol.style.Fill({
                                    color: 'rgba(255, 255, 255, 0.5)'
                                })
                            });
                        }
                        else return modifystyles;
                        
                    }
                });
                this.interactionSelect.getFeatures().clear();
                this.map.removeInteraction(this.interactionSelect);
                //this.map.addInteraction(interactionSelectPointerMove);
                this.map.addInteraction(this.interactionSelect);

                // create the interaction
                this.draw_interaction = null;
                // if there is only one feature, modify this
                this.draw_interaction = new ol.interaction.Modify({
                    features: this.interactionSelect.getFeatures(),
                    geometryName: sqlcolumn,
                });
                // if the user already selected a element, start to edit with this
                if (this.selectedfeature != null) {
                    var features = self.interactionSelect.getFeatures();
                    features.push(this.selectedfeature);
                }

                var self = this;
                this.drawsource.once('change', function (e) {
                    

                    if (GeoWebGIS.drawsource.getFeatures().length == 1) {
                        var selectedfeatures;
                        selectedfeatures = GeoWebGIS.drawsource.getFeatures()[0];
                        var features = self.interactionSelect.getFeatures();
                        // now you have an ol.Collection of features that you can add features to
                        features.push(selectedfeatures);
                    }

                });

                // add it to the map
                this.map.addInteraction(this.draw_interaction);

                this.interactionSelect.getFeatures().on('add', function (e) {

                    e.element.on('change', function (e) {
                        dirty[e.target.getId()] = true;
                    });
                });
                this.interactionSelect.getFeatures().on('remove', function (e) {
                    var f = e.element;
                    if (dirty[f.getId()]) {
                        var parser = new jsts.io.OL3Parser();

                        var jstsGeom = parser.read(f.getGeometry());

                        if (jstsGeom.isValid()) {

                            delete dirty[f.getId()];
                            var featureProperties = f.getProperties();
                            delete featureProperties.boundedBy;
                            var clone = new ol.Feature(featureProperties);
                            clone.setId(featureProperties.geometryid);
                            clone.setGeometryName(sqlcolumn);

                            self.saveData('update', clone);
                        }
                        else {
                            /*BootstrapDialog.show({
                                type: BootstrapDialog.TYPE_DANGER,
                                title: GeoWebGIS.translator.get("wrongPolyTitle"),
                                message: GeoWebGIS.translator.get("wrongPolyText") + '<img src="/Content/themes/invalid_polygon.png" width="560" />',
                                buttons: [{
                                    label: 'OK',
                                    action: function (dialogItself) {
                                        dialogItself.close();
                                    }
                                }]
                            });*/
                            // TODO Fehlermeldung invalid Polygon

                        }

                        GeoWebGIS.endDrawInteraction();

                    }
                });
                var snap = new ol.interaction.Snap({
                    source: this.drawsource
                });
                this.map.addInteraction(snap);
            }
        }

        GeoWebGIS.endDrawInteraction = function () {
            this.isDrawing = false;
            if (this.interactionSelect) this.interactionSelect.getFeatures().clear();
            this.drawlayer.getSource().clear();
            this.map.removeLayer(this.drawlayer);
            this.map.removeInteraction(this.draw_interaction);
            //GeoWebGIS.map.addLayer(this.geojsonlayer);
            this.map.addInteraction(this.clusterselect);
            //GeoWebGIS.showWFSLayer(GeoWebGIS.wfsurl);

            $(editPolygon.getButtonElement()).blur();
            $(editLine.getButtonElement()).blur();
            $(editPoint.getButtonElement()).blur();

            editbartoggle.setActive(false);
        }

        GeoWebGIS.saveData = function (mode, newFeature) {

            var self = this;

            var formatWFS = new ol.format.WFS();

            var formatGML;

            formatGML = new ol.format.GML({
                featureNS: GeoWebGIS.workspace,
                featureType: 'geometries',
                srsName: 'EPSG:21781'
            });


            var xs = new XMLSerializer();
            var param = "";
            // geoserver Arbeitsbereich
            var workbench = GeoWebGIS.workspace;

            var node;
            switch (mode) {
                case 'insert':
                    node = formatWFS.writeTransaction([newFeature], null, null, formatGML);
                    break;
                case 'update':
                    node = formatWFS.writeTransaction(null, [newFeature], null, formatGML);
                    break;
                case 'delete':
                    node = formatWFS.writeTransaction(null, null, [newFeature], formatGML);
                    break;
            }

            var payload = xs.serializeToString(node);
            $.ajax('/MapImageProxy/GetGeoServer?param=' + param + '&workbench=' + workbench, {
                service: 'WFS',
                type: 'POST',
                dataType: 'json',
                processData: false,
                contentType: 'json',
                data: payload,
                success: function (data) {
                    try {
                        var xmlDoc = $.parseXML(data);
                        var $xml = $(xmlDoc);
                        var $title1 = $xml.children()[0];
                        var t2 = $($title1).children()[2];
                        var t3 = $(t2).children().children();
                        var geometryid = $(t3).attr('fid').replace("geometries.", "");
                        console.log(geometryid);
                        console.log(newFeature);
                        if (geometryid == "none") {
                            // Update --> 
                            $.get("/ReferenceGeometry/AddChangelog/" + newFeature.getId() + "?update=true");
                        }
                        else  $.get("/ReferenceGeometry/AddChangelog/" + geometryid);
                    }
                    finally {
                        GeoWebGIS.reloadGeoJSON();
                    }
                    
                }
            });

        }



        return GeoWebGIS;
    }
    //define globally if it doesn't already exist
    if (typeof (GeoWebGIS) === 'undefined') {
        window.GeoWebGIS = define_GeoWebGIS();
        window.GeoWebGIS.initialize();
    }
    else {
        console.log("GeoWebGIS already defined.");
    }
})(window);