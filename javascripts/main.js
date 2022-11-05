///<script>

var app = angular.module("myApp", ["ngRoute", "projectControllers"]);
app.config(function ($routeProvider) {
    $routeProvider
        .when("/", {
            templateUrl: "content/moved.html"
        })
        .when("/gettingstarted", {
            templateUrl: "content/moved.html"
        })
        .when("/walkthrough-menu", {
            templateUrl: "content/moved.html"
        })
        .when("/walkthrough", {
            templateUrl: "content/moved.html"
        })
        .when("/walkthroughcore", {
            templateUrl: "content/moved.html"
        })
        .when("/walkthroughcore2", {
            templateUrl: "content/moved.html"
        })
        .when("/walkthroughcore3", {
            templateUrl: "content/moved.html"
        })
        .when("/walkthroughcore4", {
            templateUrl: "content/moved.html"
        })
        .when("/walkthroughcore5", {
            templateUrl: "content/moved.html"
        })
        .when("/ragrs", {
            templateUrl: "content/moved.html"
        })
        .when("/techoverview-menu", {
            templateUrl: "content/moved.html"
        })
        .when("/techoverview", {
            templateUrl: "content/moved.html"
        })
        .when("/techoverview2", {
            templateUrl: "content/moved.html"
        })
        .when("/techoverview3", {
            templateUrl: "content/moved.html"
        })
        .when("/migration", {
            templateUrl: "content/moved.html"
        })
        .otherwise({
            templateUrl: "content/moved.html"
        });


});



///</script>