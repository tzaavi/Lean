
var app = app || {};
app.optimization = app.optimization || {};


app.optimization.TestModel = Backbone.Model.extend({
    defaults: {
        id: null,
        vars: null
    }
});

app.optimization.TestList = Backbone.Collection.extend({
    model: app.optimization.TestModel,
    url: '/api/tests'
});


app.optimization.MainView = Backbone.Marionette.ItemView.extend({
    el: '#main-view',
    template: false,
    data: {},

    ui: {
        ddDimentions: '#dd-dimentions',
        ddParameter1: '#dd-parameter1',
        ddParameter2: '#dd-parameter2',
        tblStats: '#tbl-stats',
        chartWrapper: '#chart-wrapper'
    },

    events: {
        'change @ui.ddDimentions': 'renderChart',
        'change @ui.ddParameter1': 'renderChart',
        'change @ui.ddParameter2': 'renderChart'
    },

    initialize: function() {
        console.log('main view: init');
    },

    onRender: function() {
        console.log('main view: onRender');
        var self = this;

        $.getJSON('/api/optimization/dimentions', function(data) {
            console.log('dimentions', data);
            var dimDict = {};
            _.each(data, function(item) {
                dimDict[item.key] = item;
                $('#dd-dimentions').append('<option value="' + item.key + '">' + item.category + '.' + item.name + '</option>');
            });
            console.log('dimDict', dimDict)
            self.data.dimDict = dimDict;
        });

        $.getJSON('/api/optimization/parameters', function(data) {
            console.log('parameters', data);
            _.each(data, function(item) {
                // need to lower case the first letter to mach the json data returned from server
                var val = item.name.charAt(0).toLowerCase() + item.name.slice(1);
                $(self.ui.ddParameter1).append('<option value="' + val + '">' + item.name + '</option>');
                $(self.ui.ddParameter2).append('<option value="' + val + '">' + item.name + '</option>');
            });
        });

        $.getJSON('/api/optimization/stats', function(data) {
            console.log('stats', data);
            self.data.stats = data;
            // make sure we have dimDict before render
            var int = setInterval(function() {
                if (self.data.dimDict) {
                    self.renderTable(data);
                    clearInterval(int);
                }
            }, 1);

        })
    },

    renderTable: function(stats) {
        if (stats == null || stats.length == 0)
            return;
        var self = this;

        // render header
        var first = stats[0];
        var trHead = $('<tr><th>Exe</th></td>');
        _.each(first.parameters, function(val, key) {
            trHead.append('<th>' + key + '</th>');
        });
        _.each(first.values, function(val, key) {
            if (key.indexOf('summary') == 0)
                trHead.append('<th>' + self.data.dimDict[key].name + '</th>');
        });
        self.ui.tblStats.find('thead').append(trHead);

        // render body
        _.each(stats, function(item) {
            var tr = $('<tr><td>' + item.exId + '</td></tr>');
            _.each(item.parameters, function(val, key) {
                tr.append('<td>' + val + '</td>');
            });
            _.each(item.values, function(val, key) {
                if (key.indexOf('summary') == 0)
                    tr.append('<td>' + val + '</td>');
            });
            self.ui.tblStats.find('tbody').append(tr);
        });
    },

    renderChart: function() {
        var self = this;
        var param1 = this.ui.ddParameter1.val();
        var param2 = this.ui.ddParameter2.val();
        var dim = this.ui.ddDimentions.val();
        console.log('renderChart', dim, param1, param2);

        if (param2 != '' && param2 != param1) {
            self.renderChartTwoParam(param1, param2, dim);
        } else {
            self.renderChartOneParam(param1, dim);
        }

    },

    renderChartOneParam: function(param1, dim) {
        var self = this;
        var series = {
            name: param1,
            data: []
        };
        series.data = _.map(self.data.stats, function(item) {
            return {
                x: item.parameters[param1],
                y: item.values[dim]
            }
        });
        console.log('series', series);

        var options = {
            chart: {
                type: 'scatter',
                zoomType: 'xy'
            },
            title: {
                text: dim
            },
            xAxis: {
                title: {
                    enabled: true,
                    text: param1
                },
                startOnTick: true,
                endOnTick: true,
                showLastLabel: true,
                min: 0
            },
            yAxis: {
                title: {
                    text: dim
                }
            },
            legend: {
                layout: 'vertical',
                align: 'right',
                verticalAlign: 'top',
                floating: true,
                borderWidth: 1
            },
            /*plotOptions: {
                scatter: {
                    tooltip: {
                        headerFormat: '<b>{series.name}</b><br>',
                        pointFormat: type + ': {point.x} <br>Profit/Loss: {point.y} <br>Duration: {point.duration} <br> Time: {point.time}'
                    }
                }
            },*/
            series: [series]
        };
        self.ui.chartWrapper.highcharts(options);
    },

    renderChartTwoParam: function(param1, param2, dim) {
        var self = this;
        self.ui.chartWrapper.empty();
        var data = [{
            type: 'scatter3d',
            mode: "markers",
            x: _.map(self.data.stats, function(item) {
                return item.parameters[param1];
            }),
            y: _.map(self.data.stats, function(item) {
                return item.parameters[param2];
            }),
            z: _.map(self.data.stats, function(item) {
                return item.values[dim];
            })
        }];
        console.log('plotly', data);
        var layout = {
            title: 'Mt Bruno Elevation',
            autosize: false,
            //width: 500,
            //height: 500,
            margin: {
                l: 65,
                r: 50,
                b: 65,
                t: 90
            }
        };
        Plotly.newPlot(self.ui.chartWrapper.get(0), data, layout);
    }
});
