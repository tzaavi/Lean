var app = {};
app.execution = app.execution || {};


app.execution.Model = Backbone.Model.extend({
    defaults: {
        executionId: null,
        stats: null,
        trades: null,
        chartList: null,
        chartData: null
    },

    initialize: function(){
        var self = this;
        var exId = this.get('executionId');
        $.getJSON('/api/executions/' + exId + '/stats', function(stats) {
            self.onStatLoaded(stats);
        });
        $.getJSON('/api/executions/' + exId + '/trades', function(trades) {
            self.set('trades', trades);
        });
        $.getJSON('/api/executions/' + exId + '/charts', function(charts) {
            self.set('chartList', charts);
        });
    },

    onStatLoaded: function(stats){
        var self = this;
        var stats = _.map(stats, function(item){
            if(item.key.indexOf('time') != -1)
                item.value = moment.utc(item.value * 1000).format();
            return item;
        });
        obj = {};
        obj.summary = _.filter(stats, function(item){
            return item.key.indexOf('summary') == 0;
        });
        obj.trade = _.filter(stats, function(item){
            return item.key.indexOf('trade') == 0;
        });
        obj.portfolio = _.filter(stats, function(item){
            return item.key.indexOf('portfolio') == 0;
        });
        self.set('stats', obj);
    },

    loadChartData: function(chartId) {
        var self = this;
        var exId = this.get('executionId');
        $.getJSON('/api/executions/' + exId + '/charts/' + chartId, function(chart) {
            chart = self.alterOhlcSeries(chart)
            self.set('chartData', chart);
        });
    },

    alterOhlcSeries: function(chart){
        var open = null;
        var high = null;
        var close = null;
        var low = null;
        _.each(chart.series, function(s) {
            if(s.name == 'Open') { open = s.points; }
            if(s.name == 'High') { high = s.points; }
            if(s.name == 'Low') { low = s.points; }
            if(s.name == 'Close') { close = s.points; }
        });
        if(open && high && close && low) {
            var ohlc = _.map(open, function(item, i){
                return {
                    time: item.time,
                    o: item.value,
                    h: high[i].value,
                    l: low[i].value,
                    c: close[i].value
                };
            });
            var newSeries = [];
            newSeries.push({
                name: 'OHLC',
                points: ohlc
            });
            _.each(chart.series, function(s) {
                if(s.name != 'Open' && s.name != 'High' && s.name != 'Low' && s.name != 'Close') {
                    newSeries.push(s);
                }
            });
            chart.series = newSeries;
        }
        return chart;
    }
});



app.execution.MainView = Backbone.Marionette.ItemView.extend({
    el: '#main-view',
    template: false,
    data: {},
    tradeMarkerSeries: [],
    model: null,

    ui: {
        tblTrades: '#trades-table tbody',
        menuItemChart: '#charts-dropdown a.menu-item-chart',
        menuItemAnalyze: '#analyze-dropdown a'
    },

    events: {
        'click @ui.menuItemChart': 'onChartMenuItemClick',
        'click @ui.menuItemAnalyze': 'onMeneItemAnalyzeClick'
    },

    modelEvents: {
        'change:stats': 'onStatChanged',
        'change:trades': 'renderTradeTable',
        'change:chartList': 'renderChartMenu',
        'change:chartData': 'renderChart',
        'chartUpdate': 'onChartUpdate'
    },

    initialize: function(){
        console.log('main view: init');
        var self = this;
        self.model = new app.execution.Model({executionId: self.options.executionId});

        //todo: refactor
        $('#btn-show-trade-markers').click(function () {
            self.addTradeMarkers();
            $('#btn-show-trade-markers').hide();
            $('#btn-hide-trade-markers').show();
        });

        $('#btn-hide-trade-markers').click(function () {
            self.removeTradeMarkers();
            $('#btn-show-trade-markers').show();
            $('#btn-hide-trade-markers').hide();
        });
    },

    onStatChanged: function(model, stats) {
        var stats = this.model.get('stats');
        this.renderStatListInTable(stats.summary, $('#statistics'));
        this.renderStatListInTable(stats.trade, $('#trade-stats-table'));
        this.renderStatListInTable(stats.portfolio, $('#potfolio-stats-table'));
    },

    onChartMenuItemClick: function(e){
        var chartId = $(e.currentTarget).data('chartId');
        console.log('menu item click', chartId);
        this.model.loadChartData(chartId);
        $('#btn-show-trade-markers').show();
        $('#btn-hide-trade-markers').hide();
    },

    onMeneItemAnalyzeClick: function(e){
        var type = $(e.currentTarget).data('type');
        console.log('analyze click:', type);
        if (type == "mae" || type == "mfe") {
            this.renderMaxExcursion(type);
        }
    },

    renderTradeTable: function(model, trades){
        var self = this;
        _.each(trades, function(v){
            tr = $('<tr></tr>');
            tr.append('<td>' + v.symbol + '</td>');
            tr.append('<td>' + moment.utc(v.entryTime * 1000).format('YYYY-MM-DD HH:mm') + '</td>');
            tr.append('<td>' + v.entryPrice + '</td>');
            tr.append('<td>' + app.helpers.getOrderDirection(v.direction) + '</td>');
            tr.append('<td>' + v.quantity + '</td>');
            tr.append('<td>' + moment.utc(v.exitTime * 1000).format('YYYY-MM-DD HH:mm') + '</td>');
            tr.append('<td>' + v.exitPrice + '</td>');
            tr.append('<td>' + v.profitLoss + '</td>');
            tr.append('<td>' + v.totalFees + '</td>');
            tr.append('<td>' + v.mae + '</td>');
            tr.append('<td>' + v.mfe + '</td>');
            tr.append('<td>' + v.duration + '</td>');
            tr.append('<td>' + v.endTradeDrawdown + '</td>');
            self.ui.tblTrades.append(tr);
        });
    },

    renderStatListInTable: function(list, table) {
        var i = 0;
        var tr = null;
        _.each(list, function(item, i) {
            if (i % 2 == 0) {
                tr = $('<tr></tr>');
                table.append(tr);
            }
            i++;
            tr.append('<td>' + item.name + '</td><td>' + item.value + (item.unit != null ? item.unit : '') + '</td>');
        });
    },

    renderOrderList: function () {
        var orders = _data['Orders'];
        var tbody = $('#orders-table tbody');
        $.each(orders, function (k, v) {
            tr = $('<tr></tr>');
            tr.append('<td>' + v.Time + '</td>');
            tr.append('<td>' + v.Symbol.Value + '</td>');
            tr.append('<td>' + v.Price + '</td>');
            tr.append('<td>' + app.helpers.getOrderType(v.Type) + '</td>');
            tr.append('<td>' + v.Quantity + '</td>');
            tr.append('<td>' + app.helpers.getOrderDirection(v.Direction) + '</td>');
            tr.append('<td>' + app.helpers.getOrderStatus(v.Status) + '</td>');
            tr.append('<td>' + v.Tag + '</td>');
            tbody.append(tr);
        });
    },

    renderChartMenu: function(model, charts){
        var self = this;
        var ul = $('#charts-dropdown');
        _.each(charts, function(item){
            if(item.name == 'Strategy Equity') {
                self.model.loadChartData(item.id)
            }
            var a = $('<a href="#" class="menu-item-chart">' + item.name + '</a>');
            a.data('chartId', item.id);
            var li = $('<li></li>');
            li.append(a);
            ul.prepend(li);
        });
    },

    renderMaxExcursion: function (type) {
        var profit = {
            name: 'Profitable Trade',
            color: 'green',
            data: []
        }
        var loss = {
            name: 'Losing Trade',
            color: 'red',
            data: []
        }
        _.each(this.model.get('trades'), function (item) {
            if (item["profitLoss"] >= 0) {
                profit.data.push({
                    x: Math.abs(item[type]),
                    y: Math.abs(item["profitLoss"]),
                    duration: item["duration"],
                    time: item["entryTime"]
                });
            } else {
                loss.data.push({
                    x: Math.abs(item[type]),
                    y: Math.abs(item["profitLoss"]),
                    duration: item["duration"],
                    time: item["dntryTime"]
                });
            }
        });

        var options = {
            chart: {
                type: 'scatter',
                zoomType: 'xy'
            },
            title: {
                text: type
            },
            xAxis: {
                title: {
                    enabled: true,
                    text: type
                },
                startOnTick: true,
                endOnTick: true,
                showLastLabel: true,
                min: 0
            },
            yAxis: {
                title: {
                    text: 'Prifit/Loss'
                }
            },
            legend: {
                layout: 'vertical',
                align: 'right',
                verticalAlign: 'top',
                floating: true,
                borderWidth: 1
            },
            plotOptions: {
                scatter: {
                    tooltip: {
                        headerFormat: '<b>{series.name}</b><br>',
                        pointFormat: type + ': {point.x} <br>Profit/Loss: {point.y} <br>Duration: {point.duration} <br> Time: {point.time}'
                    }
                }
            },
            series: [profit, loss]
        };

        $('#chart-wrapper').highcharts(options);
    },

    addTradeMarkers: function () {
        var tradesEntry = {
            name: 'Trade',
            type: 'scatter',
            data: [],
            tooltip: {
                headerFormat: '<b>Open trade</b><br>',
                pointFormat: 'ID: {point.id} <br>Time: {point.x} <br>Price: {point.y} <br> Direction: {point.direction} <br> Quantity: {point.quantity}',
                formatter: function (){ return 'Time: ' + moment(this.x).format() }
            }
        };
        tradesEntry.data = _.map(this.model.get('trades'), function (t, i) {
            return {
                x: moment(t.entryTime * 1000).toDate(),
                y: t.entryPrice,
                direction: app.helpers.getOrderDirection(t.direction),
                quantity: t.quantity,
                id: i + 1
            }
        });

        var tradesExit = {
            bane: 'Trade',
            type: 'scatter',
            data: [],
            tooltip: {
                headerFormat: '<b>Close trade</b><br>',
                pointFormat: 'ID: {point.id} <br>Time: {point.x} <br>Price: {point.y} <br> Direction: {point.direction} <br> Quantity: {point.quantity}',
                formatter: function () { return 'Time: ' + moment(this.x).format() }
            }
        };
        tradesExit.data = _.map(this.model.get('trades'), function (t, i) {
            return {
                x: moment(t.exitTime * 1000).toDate(),
                y: t.exitPrice,
                direction: app.helpers.getOrderDirection(t.direction),
                quantity: t.quantity,
                id: i + 1
            }
        });



        var chart = $('#chart-wrapper').highcharts();
        this.tradeMarkerSeries.push(chart.addSeries(tradesEntry));
        this.tradeMarkerSeries.push(chart.addSeries(tradesExit));
    },

    removeTradeMarkers: function () {
        for (var i = 0; i < this.tradeMarkerSeries.length; i++) {
            this.tradeMarkerSeries[i].remove(true);
        }
        this.tradeMarkerSeries = [];
    },

    toHighchartSeries: function(series) {
        var newSeries = $.map(series, function (s) {
            var track = {
                name: s.name,
                type: app.helpers.getSeriesType(s.seriesType),
                symbol: 'none'
            };
            track.data = [];

            if(s.name == 'OHLC') {
              track.type = 'candlestick'
            }

            _.each(s.points, function (item) {
                var time;
                if (isNaN(item.time)) {
                    time = moment(item.time).toDate();
                } else {
                    time = item.time * 1000;
                }
                if(track.type == 'candlestick') {
                  track.data.push([time, item.o, item.h, item.l, item.c]);
                } else {
                  track.data.push([time, item.value]);
                }
            });
            return track;
        });
        return newSeries;
    },

    renderChart: function (model, chart) {
        var self = this;
        var chartId = chart.id;

        // chart options
        var options = {
            rangeSelector: {
                buttons: [{
                  type: 'day',
                  count: 3,
                  text: '3d'
              }, {
                  type: 'week',
                  count: 1,
                  text: '1w'
              }, {
                  type: 'month',
                  count: 1,
                  text: '1m'
              }, {
                  type: 'month',
                  count: 6,
                  text: '6m'
              }, {
                  type: 'year',
                  count: 1,
                  text: '1y'
              }, {
                  type: 'all',
                  text: 'All'
              }],
              selected: 3
            },

            chart: {
                zoomType: 'xy'
            },

            scrollbar: {
                liveRedraw: true
            },

            rangeSelector: {
                buttons: [
                    {
                        type: 'hour',
                        count: 1,
                        text: 'hour'
                    }, {
                        type: 'day',
                        count: 1,
                        text: 'day'
                    }, {
                        type: 'month',
                        count: 1,
                        text: '1m'
                    }, {
                        type: 'month',
                        count: 6,
                        text: '6m'
                    }, {
                        type: 'ytd',
                        text: 'YTD'
                    }, {
                        type: 'year',
                        count: 1,
                        text: '1y'
                    }, {
                        type: 'all',
                        text: 'All'
                    }
                ]
            }
        }

        // get data by chart name
        var series1 = self.toHighchartSeries(chart.series);

        // construct plots base on chart type (stack, overlay)
        if (chart.chartType == 0) { // overlay
            this.plots = [{ series: series1 }];
        }

        if (chart.chartType == 1) { // stack
            this.plots = $.map(series1, function (item) {
                return { series: [item] };
            });
        }
        console.log('plots: ', this.plots);

        // calc height
        var plotHeight = 100 / this.plots.length;

        // generate y axis
        var yAxis = $.map(this.plots, function (item, i) {
            var name = $.map(item.series, function (item) {
                return item.name
            }).join();
            return {
                labels: {
                    align: 'right',
                    x: -3
                },
                title: {
                    text: name
                },
                height: plotHeight + '%',
                top: (plotHeight * i) + '%',
                offset: 0,
                lineWidth: 2
            };
        });

        // generate series
        var series = [];
        for (var i = 0; i < this.plots.length; i++) {
            for (var j = 0; j < this.plots[i].series.length; j++) {
                var s = this.plots[i].series[j];
                s.yAxis = i;
                /*s.dataGrouping = {
                    enabled:false
                };*/
                series.push(s);
            }
        }

        console.log('yaxis', yAxis);
        console.log('char series', series);

        // set options
        options.yAxis = yAxis;
        options.series = series;


        // render the chart
        $('#chart-wrapper').highcharts('StockChart', options);
    },

    onChartUpdate: function(data){
        var chart = $('#chart-wrapper').highcharts();
        chart.hideLoading();
        var series = this.toHighchartSeries(data.series);
        var dict = _.object(_.map(series, function(x){return x.name}), _.map(series, function(x){return x.data}));
        _.each(chart.series, function(s){
            if(dict[s.name])
                s.setData(dict[s.name]);
        });
    }
});




//----------------------------------------------------------------
// Helper methods
//----------------------------------------------------------------
(function (app) {
    app.helpers = {
        getSeriesType: function (id) {
            switch (id) {
                case 0:
                    return 'line';
                case 1:
                    return 'scatter';
                case 2:
                    return 'line';
                case 3:
                    return 'area'; // should be column but it has some issue render properly
                default:
                    return 'line';
            }
        },

        getOrderType: function (id) {
            switch (id) {
                case 0:
                    return 'Market';
                case 1:
                    return 'Limit';
                case 2:
                    return 'StopMarket';
                case 3:
                    return 'StopLimit';
                case 4:
                    return 'MarketOnOpen';
                case 5:
                    return 'MarketOnClose';
                default:
                    return '';
            }
        },

        getOrderDirection: function (id) {
            switch (id) {
                case 0:
                    return 'Buy';
                case 1:
                    return 'Sell';
                case 2:
                    return 'Hold';
                default:
                    return '';
            }
        },

        getOrderStatus: function (id) {
            switch (id) {
                case 0:
                    return 'New';
                case 1:
                    return 'Submitted';
                case 2:
                    return 'PartiallyFilled';
                case 3:
                    return 'Filled';
                case 5:
                    return 'Canceled';
                case 6:
                    return 'None';
                case 7:
                    return 'Invalid';
                default:
                    return '';
            }
        }
    };
})(app);


//----------------------------------------------------------------
// Start
//----------------------------------------------------------------
$(function () {
    // set main split
    $('#main').split({
        orientation: 'horizontal',
        limit: 20,
        position: '60%',
        onDrag: function () {
            var chart = $('#chart-wrapper').highcharts();
            var w = $('#chart-wrapper').width();
            var h = $('#chart-wrapper').height();
            console.log('redraw', w, h);
            chart.setSize(w, h)
        }
    });

    Highcharts.setOptions({
        global: {
            useUTC: true
        }
    });
});
