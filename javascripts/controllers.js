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

    });

});
