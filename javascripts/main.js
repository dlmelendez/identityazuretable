///<script>

var app = angular.module("myApp", ["ngRoute", "projectControllers"]);
app.config(function($routeProvider) {
    $routeProvider
    .when("/", {
        templateUrl: "content/index.html"
    })
    .when("/gettingstarted", {
        templateUrl: "content/gettingstarted.html"
    })
    .when("/walkthrough-menu", {
        templateUrl: "content/walkthrough-menu.html"
    })
    .when("/walkthrough", {
        templateUrl: "content/walkthrough.html"
    })
    .when("/walkthroughcore", {
        templateUrl: "content/walkthroughcore.html"
    })
    .when("/walkthroughcore2", {
        templateUrl: "content/walkthroughcore2.html"
    })
    .when("/walkthroughcore3", {
        templateUrl: "content/walkthroughcore3.html"
    })
    .when("/walkthroughcore4", {
        templateUrl: "content/walkthroughcore4.html"
    })
    .when("/ragrs", {
        templateUrl: "content/ragrs.html"
    })
    .when("/techoverview-menu", {
        templateUrl: "content/techoverview-menu.html"
    })
    .when("/techoverview", {
        templateUrl: "content/techoverview.html"
    })
    .when("/techoverview2", {
        templateUrl: "content/techoverview2.html"
    })
    .when("/techoverview3", {
        templateUrl: "content/techoverview3.html"
    })
    .when("/migration", {
        templateUrl: "content/migration.html",
        controller: "migrationDetails"
    })
    .otherwise({
        templateUrl : "content/index.html"
    });

    $rootScope.$watch('$stateChangeSuccess', function (event) {
        ga('set', 'page', $location.path());
        ga('send', 'pageview');
    });
});

///</script>