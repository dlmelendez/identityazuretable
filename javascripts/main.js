///<script>

var app = angular.module("myApp", ["ngRoute"]);
app.config(function($routeProvider) {
    $routeProvider
    .when("/", {
        templateUrl: "/content/index.html"
    })
    .when("/gettingstarted", {
        templateUrl: "/content/gettingstarted.html"
    })
    .when("/walkthrough", {
        templateUrl: "/content/walkthrough.html"
    })
    .when("/walkthroughcore", {
        templateUrl: "/content/walkthroughcore.html"
    })
    .when("/ragrs", {
        templateUrl: "/content/ragrs.html"
    })
    .when("/techoverview", {
        templateUrl: "/content/techoverview.html"
    })
    .otherwise({
        templateUrl : "/content/index.html"
    });
});

///</script>