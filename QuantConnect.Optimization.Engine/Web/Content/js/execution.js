var app = {};

//----------------------------------------------------------------
// Helper methods
//----------------------------------------------------------------
(function (app) {
    app.helpers = {
        getSeriesFromQuantConnectOrders: function () {
            var ordersSeries = {};
            $.each(_data['Orders'], function (k, v) {
                var key = v.Symbol.Value + '_' + app.helpers.getOrderDirection(v.Direction);
                var series = ordersSeries[key];
                if (!series) {
                    series = {
                        name: key,
                        type: 'scatter',
                        data: []
                    };

                    var symbol = 'triangle';
                    if (v.Direction == 1)
                        symbol = 'triangle-down';

                    ordersSeries[key] = series;
                }

                var time = v.Time;
                if (time.indexOf('Z') == -1)
                    time += 'Z';
                series.data.push([
                    moment(time).valueOf(),
                    v.Price
                ]);
            });
            var series = [];
            $.each(ordersSeries, function (k, v) {
                series.push(v);
            });
            return series;
        },

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
// View
//----------------------------------------------------------------
(function (app) {
    app.view = {
        chartsDropdown: null,

        init: function () {
            this.renderChartMenu();

            $('#analyze-dropdown a').click(function () {
                var type = $(this).data('type');
                console.log('analyze click:', type);
                if (type == "MAE" || type == "MFE") {
                    app.chartRender.renderMaxExcursion(type);
                }

            });

            $('#btn-show-trade-markers').click(function () {
                app.chartRender.addTradeMarkers();
                $('#btn-show-trade-markers').hide();
                $('#btn-hide-trade-markers').show();
            });

            $('#btn-hide-trade-markers').click(function () {
                app.chartRender.removeTradeMarkers();
                $('#btn-show-trade-markers').show();
                $('#btn-hide-trade-markers').hide();
            });

        },

        renderChartMenu: function(){
            $.getJSON('/api/executions/' + _executionId + '/charts', function(charts) {
                console.log('charts--', charts);
                var ul = $('#charts-dropdown');
                _.each(charts, function(item){
                    var a = $('<a href="#">' + item.name + '</a>');
                    var li = $('<li></li>');
                    li.append(a);
                    ul.prepend(li);
                    a.click(function () {
                        console.log('click', item.name, item.id);
                        app.chartRender.laod(item.id);
                        $('#btn-show-trade-markers').show();
                        $('#btn-hide-trade-markers').hide();
                    });
                });
            });
        }

    };
})(app);

//----------------------------------------------------------------
// Chart Render
//----------------------------------------------------------------
(function (app) {
    app.chartRender = {
        plots: [],

        laod: function(chartId) {
            var self = this;
            $.getJSON('/api/executions/' + _executionId + '/charts/' + chartId, function(chart) {
                console.log('chart data', chart);
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
                        if(s.name != 'Open' && s.name != 'High' && s.name != 'Low' && s.name && 'Close') {
                            newSeries.push(s);
                        }
                    });
                    chart.series = newSeries;
                }
                console.log('chart data after ohlc', chart);
                self.render(chart)
            });
        },

        render: function (chart) {
            var self = this;

            // chart options
            var options = {
                rangeSelector: {
                    selected: 1
                },

                chart: {
                    zoomType: 'xy'
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
            var series1 = $.map(chart.series, function (s) {
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
            console.log('chart series: ', series1);

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
                    s.dataGrouping = {
                        enabled:false
                    };
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

        tradeMarkerSeries: [],

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
            tradesEntry.data = $.map(_data["TotalPerformance"]["ClosedTrades"], function (t, i) {
                return {
                    x: moment(t.EntryTime).toDate(),
                    y: t.EntryPrice,
                    direction: app.helpers.getOrderDirection(t.Direction),
                    quantity: t.Quantity,
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
            tradesExit.data = $.map(_data["TotalPerformance"]["ClosedTrades"], function (t, i) {
                return {
                    x: moment(t.ExitTime).toDate(),
                    y: t.ExitPrice,
                    direction: app.helpers.getOrderDirection(t.Direction),
                    quantity: t.Quantity,
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
            $.each(_data["TotalPerformance"]["ClosedTrades"], function (i, item) {
                if (item["ProfitLoss"] >= 0) {
                    profit.data.push({
                        x: Math.abs(item[type]),
                        y: Math.abs(item["ProfitLoss"]),
                        duration: item["Duration"],
                        time: item["EntryTime"]
                    });
                } else {
                    loss.data.push({
                        x: Math.abs(item[type]),
                        y: Math.abs(item["ProfitLoss"]),
                        duration: item["Duration"],
                        time: item["EntryTime"]
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

        resizeAllPlots: function () {
            var chart = $('#chart-wrapper').highcharts();
            var w = $('#chart-wrapper').width();
            var h = $('#chart-wrapper').height();
            console.log('redraw', w, h);
            chart.setSize(w, h);
        }
    };
})(app);


//----------------------------------------------------------------
// Summary render
//----------------------------------------------------------------
(function (app) {
    app.statRender = {
        renderPerformance: function () {
            var self = this;
            $.getJSON('/api/executions/' + _executionId + '/stats', function(stats) {
                console.log('stats', stats);
                var stats = _.map(stats, function(item){
                    if(item.key.indexOf('time') != -1)
                        item.value = moment.utc(item.value * 1000).format();
                    return item;
                });
                var summary = _.filter(stats, function(item){
                    return item.key.indexOf('summary') == 0;
                });
                var trade = _.filter(stats, function(item){
                    return item.key.indexOf('trade') == 0;
                });
                var portfolio = _.filter(stats, function(item){
                    return item.key.indexOf('portfolio') == 0;
                });
                self.renderStatListInTable(summary, $('#statistics'));
                self.renderStatListInTable(trade, $('#trade-stats-table'));
                self.renderStatListInTable(portfolio, $('#potfolio-stats-table'));
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

        renderTradeList: function () {
            $.getJSON('/api/executions/' + _executionId + '/trades', function(trades) {
                console.log('trades', trades);
                var tbody = $('#trades-table tbody');
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
                    tbody.append(tr);
                });
            });
        }
    }
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
            app.chartRender.resizeAllPlots();
        }
    });

    Highcharts.setOptions({
        global: {
            useUTC: true
        }
    });

    // init view
    app.view.init();

    // fixed chart type for Strategy Equity data and order the series
    _data.Charts['Strategy Equity'].ChartType = 1;

    // if chart contains OHLC data convert to candle series
    $.each(_data.Charts, function(key, chart) {
        var series = chart.Series;
        if (series.hasOwnProperty('Open') && series.hasOwnProperty('High') && series.hasOwnProperty('Low') && series.hasOwnProperty('Close')) {
            var ohlc = $.map(series['Open'].Values, function(val, i) {
                return {
                    time: val.time,
                    o: val.value,
                    h: series['High'].Values[i].value,
                    l: series['Low'].Values[i].value,
                    c: series['Close'].Values[i].value
                }
            });
            console.log('OHLC', ohlc);
            series['OHLC'] = {
                Name: 'OHLC',
                Values: ohlc
            }

            // remove indeviduals ohlc series
            delete series['Open'];
            delete series['High'];
            delete series['Low'];
            delete series['Close'];
        }
    });



    // render chart with cahrt builder data
    //app.chartRender.render('Strategy Equity');
    //app.chartRender.renderMaxExcursion('MFE');

    // render stats
    app.statRender.renderPerformance();
    app.statRender.renderOrderList();
    app.statRender.renderTradeList();

    // on modal save click
    $('#custom-chart-modal .btn-primary').click(function () {
        app.chartRender.render();
    });

    // apply strategy equity
    $('#btn-strategy-equity').click(function () {
        app.chartBuilder.setStrategyEquityChart();
        app.chartRender.render();
    });


});
