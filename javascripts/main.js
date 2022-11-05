///<script>

var app = angular.module("myApp", ["ngRoute", "projectControllers"]);
app.config(function ($routeProvider) {
    $routeProvider
    .when("/", {
        redirectTo: "https://elcamino.cloud/projects/docs/identityazuretable/"
    })
    .when("/gettingstarted", {
        redirectTo: "https://elcamino.cloud/projects/docs/identityazuretable/gettingstarted.html"
    })
    .when("/walkthrough-menu", {
        redirectTo: "https://elcamino.cloud/projects/docs/identityazuretable/walkthrough/index.html"
    })
    .when("/walkthrough", {
        redirectTo: "https://elcamino.cloud/projects/docs/identityazuretable/walkthrough/walkthrough.html"
    })
    .when("/walkthroughcore", {
        redirectTo: "https://elcamino.cloud/projects/docs/identityazuretable/walkthrough/walkthroughcore.html"
    })
    .when("/walkthroughcore2", {
        redirectTo: "https://elcamino.cloud/projects/docs/identityazuretable/walkthrough/walkthroughcore2.html"
    })
    .when("/walkthroughcore3", {
        redirectTo: "https://elcamino.cloud/projects/docs/identityazuretable/walkthrough/walkthroughcore3.html"
    })
    .when("/walkthroughcore4", {
        redirectTo: "https://elcamino.cloud/projects/docs/identityazuretable/walkthrough/walkthroughcore4.html"
    })
    .when("/walkthroughcore5", {
        redirectTo: "https://elcamino.cloud/projects/docs/identityazuretable/walkthrough/walkthroughcore5.html"
    })
    .when("/ragrs", {
        redirectTo: "https://elcamino.cloud/projects/docs/identityazuretable/ragrs.html"
    })
    .when("/techoverview-menu", {
        redirectTo: "https://elcamino.cloud/projects/docs/identityazuretable/techoverview/index.html"
    })
    .when("/techoverview", {
        redirectTo: "https://elcamino.cloud/projects/docs/identityazuretable/techoverview/techoverview.html"
    })
    .when("/techoverview2", {
        redirectTo: "https://elcamino.cloud/projects/docs/identityazuretable/techoverview/techoverview2.html"
    })
    .when("/techoverview3", {
        redirectTo: "https://elcamino.cloud/projects/docs/identityazuretable/techoverview/techoverview3.html"
    })
    .when("/migration", {
        redirectTo: "https://elcamino.cloud/projects/docs/identityazuretable/migration.html"
    })
    .otherwise({
        redirectTo: "https://elcamino.cloud/projects/docs/identityazuretable/"
    });

   
});



///</script>