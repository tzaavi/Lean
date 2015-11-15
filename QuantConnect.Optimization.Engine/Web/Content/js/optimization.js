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
    data:{},

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

    initialize: function () {
        console.log('main view: init');
    },

    onRender: function () {
        console.log('main view: onRender');
        var self = this;

        $.getJSON('/api/optimization/dimentions', function (data) {
            console.log('dimentions', data);
            _.each(data, function(item) {
                //todo: take care of this in server side
                var val = item.name.charAt(0).toLowerCase() + item.name.slice(1);
                $('#dd-dimentions').append('<option value="' + val + '">' + item.name + '</option>');
            });
        });

        $.getJSON('/api/optimization/parameters', function (data) {
            console.log('parameters', data);
            _.each(data, function (item) {
                //todo: take care of this in server side
                var val = item.name.charAt(0).toLowerCase() + item.name.slice(1);
                $(self.ui.ddParameter1).append('<option value="' + val + '">' + item.name + '</option>');
                $(self.ui.ddParameter2).append('<option value="' + val + '">' + item.name + '</option>');
            });
        });

        $.getJSON('/api/optimization/stats', function(data){
          console.log('stats', data);
          self.data.stats = data;
          self.renderTable(data);
        })
    },

    renderTable: function(stats){
        if(stats == null || stats.length == 0)
            return;
        var self = this;

        // render header
        var first = stats[0];
        var trHead = $('<tr><th>Test</th></td>');
        _.each(first.parameters, function(val, key){
            trHead.append('<th>' + key + '</th>');
        });
        _.each(first.values, function(val, key){
            if(key.indexOf('summary') == 0)
                trHead.append('<th>' + key.replace('summary.', '') + '</th>');
        });
        self.ui.tblStats.find('thead').append(trHead);

        // render body
        _.each(stats, function(item){
            var tr = $('<tr><td>' + item.testId + '</td></tr>');
            _.each(item.parameters, function(val, key){
                tr.append('<td>' + val + '</td>');
            });
            _.each(item.values, function(val, key){
                if(key.indexOf('summary') == 0)
                    tr.append('<td>' + val + '</td>');
            });
            self.ui.tblStats.find('tbody').append(tr);
        });
    },

    renderChart: function () {
        var self = this;
        var param1 = this.ui.ddParameter1.val();
        var param2 = this.ui.ddParameter2.val();
        var dim = this.ui.ddDimentions.val();
        console.log('renderChart', dim, param1, param2);

        var series = {
            name: param1,
            data: []
        };
        series.data = _.map(self.data.stats, function(item){
            if(param2 != '' && param2 != param1){
                return {
                    x: item.parameters[param1],
                    y: item.parameters[param2],
                    z: item.values[dim]
                }
            }else{
                return {
                    x: item.parameters[param1],
                    y: item.values[dim]
                }
            }

        });
        console.log('series', series);



        var options = {
            chart: {
                type: 'scatter',
                zoomType: 'xy',
                options3d: {
                    enabled: param2 != '',
                    alpha: 10,
                    beta: 30,
                    depth: 250,
                    viewDistance: 50,
                }
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
    }
});
