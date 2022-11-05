///<script>

var app = angular.module("myApp", ["ngRoute", "projectControllers"]);
app.config(function ($routeProvider) {
    $routeProvider
    .when("/", {
        resolveRedirectTo: () => "https://elcamino.cloud/projects/docs/identityazuretable/"
    })
    .when("/gettingstarted", {
        resolveRedirectTo: () => "https://elcamino.cloud/projects/docs/identityazuretable/gettingstarted.html"
    })
    .when("/walkthrough-menu", {
        resolveRedirectTo: () => "https://elcamino.cloud/projects/docs/identityazuretable/walkthrough/index.html"
    })
    .when("/walkthrough", {
        resolveRedirectTo: () => "https://elcamino.cloud/projects/docs/identityazuretable/walkthrough/walkthrough.html"
    })
    .when("/walkthroughcore", {
        resolveRedirectTo: () => "https://elcamino.cloud/projects/docs/identityazuretable/walkthrough/walkthroughcore.html"
    })
    .when("/walkthroughcore2", {
        resolveRedirectTo: () => "https://elcamino.cloud/projects/docs/identityazuretable/walkthrough/walkthroughcore2.html"
    })
    .when("/walkthroughcore3", {
        resolveRedirectTo: () => "https://elcamino.cloud/projects/docs/identityazuretable/walkthrough/walkthroughcore3.html"
    })
    .when("/walkthroughcore4", {
        resolveRedirectTo: () => "https://elcamino.cloud/projects/docs/identityazuretable/walkthrough/walkthroughcore4.html"
    })
    .when("/walkthroughcore5", {
        resolveRedirectTo: () => "https://elcamino.cloud/projects/docs/identityazuretable/walkthrough/walkthroughcore5.html"
    })
    .when("/ragrs", {
        resolveRedirectTo: () => "https://elcamino.cloud/projects/docs/identityazuretable/ragrs.html"
    })
    .when("/techoverview-menu", {
        resolveRedirectTo: () => "https://elcamino.cloud/projects/docs/identityazuretable/techoverview/index.html"
    })
    .when("/techoverview", {
        resolveRedirectTo: () => "https://elcamino.cloud/projects/docs/identityazuretable/techoverview/techoverview.html"
    })
    .when("/techoverview2", {
        resolveRedirectTo: () => "https://elcamino.cloud/projects/docs/identityazuretable/techoverview/techoverview2.html"
    })
    .when("/techoverview3", {
        resolveRedirectTo: () => "https://elcamino.cloud/projects/docs/identityazuretable/techoverview/techoverview3.html"
    })
    .when("/migration", {
        resolveRedirectTo: () => "https://elcamino.cloud/projects/docs/identityazuretable/migration.html"
    })
    .otherwise({
        resolveRedirectTo: () => "https://elcamino.cloud/projects/docs/identityazuretable/"
    });

   
});



///</script>