var projectControllers = angular.module('projectControllers', []);

projectControllers.controller('migrationDetails', ['$scope', '$http',
  function ($scope, $http) {
      $http.get('https://raw.githubusercontent.com/dlmelendez/identityazuretable/master/src/ElCamino.Identity.AzureTable.DataUtility/help.txt').success(function (data) {
          $scope.helptext = data;
      });

    }]);

projectControllers.controller('appController', function ($rootScope, $location) {

    $rootScope.$on('$routeChangeSuccess', function () {
        gtag('config', 'UA-136441581-2', {
            'page_path': $location.path(),
            'page_location': $location.url()
        });
        const baseRedirectUri = 'https://elcamino.cloud/projects/docs/identityazuretable';
        var redirectMap = {};
        redirectMap["/"] = baseRedirectUri;
        redirectMap["/gettingstarted"] = baseRedirectUri + "/gettingstarted.html";
        redirectMap["/walkthrough-menu"] = baseRedirectUri + "/walkthrough/index.html";
        redirectMap["/walkthrough"] = baseRedirectUri + "/walkthrough/walkthrough.html";
        redirectMap["/walkthroughcore"] = baseRedirectUri + "/walkthrough/walkthroughcore.html";
        redirectMap["/walkthroughcore2"] = baseRedirectUri + "/walkthrough/walkthroughcore2.html";
        redirectMap["/walkthroughcore3"] = baseRedirectUri + "/walkthrough/walkthroughcore3.html";
        redirectMap["/walkthroughcore4"] = baseRedirectUri + "/walkthrough/walkthroughcore4.html";
        redirectMap["/walkthroughcore5"] = baseRedirectUri + "/walkthrough/walkthroughcore5.html";
        redirectMap["/ragrs"] = baseRedirectUri + "/ragrs.html";
        redirectMap["/techoverview-menu"] = baseRedirectUri + "/techoverview/index.html";
        redirectMap["/techoverview"] = baseRedirectUri + "/techoverview/techoverview.html";
        redirectMap["/techoverview2"] = baseRedirectUri + "/techoverview/techoverview2.html"
        redirectMap["/techoverview3"] = baseRedirectUri + "/techoverview/techoverview3.html"
        redirectMap["/migration"] = baseRedirectUri + "/migration.html";

        $rootScope.localPath = $location.path();
        var redirectLookup = function (localPath) {
            return redirectMap[localPath];
        };

        $rootScope.redirectPath = redirectLookup($location.path());
        $rootScope.redirectLookup = redirectLookup;
        $rootScope.redirectTimer = setTimeout(function () {
            window.location.href = $rootScope.redirectPath;
        }, 5000);
    });

});
