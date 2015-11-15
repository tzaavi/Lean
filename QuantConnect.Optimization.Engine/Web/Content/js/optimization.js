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

    ui: {
        ddDimentions: '#dd-dimentions',
        ddParameter1: '#dd-parameter1',
        ddParameter2: '#dd-parameter2'
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
                $('#dd-dimentions').append('<option value="' + item.key + '">' + item.key + '</option>');
            });
        });

        $.getJSON('/api/optimization/parameters', function (data) {
            console.log('parameters', data);
            _.each(data, function (item) {
                $(self.ui.ddParameter1).append('<option value="' + item.name + '">' + item.name + '</option>');
                $(self.ui.ddParameter2).append('<option value="' + item.name + '">' + item.name + '</option>');
            });
        });
    },

    renderChart: function () {
        console.log('renderChart', this.ui.ddDimentions.val(), this.ui.ddParameter1.val());

        var param1 = this.ui.ddParameter1.val();
        var dim = this.ui.ddDimentions.val();
        var url = '/api/ptimization/chart?dim=' + dim + '&param1=' + param1;

        $.getJSON(url, function(data) {
            console.log(data);
        });
    }
});



