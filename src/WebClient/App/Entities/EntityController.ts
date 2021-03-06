﻿/// <reference path="../_references.ts" />
module app.controllers {
    var rowHeight = 50;

    export class EntityController extends app.base.ControllerBase {
        public static $inject = ["$scope", "$debounce", "EntityService"];
        
        constructor($scope: any, public $debounce, public entityService) {
            super();

            var textField = { field: 'text', resizable: true, displayName: 'Name', enableCellEdit: false, cellTemplate:'<span ng-bind-html="COL_FIELD"></span>' };
            var groupField = { field: 'group', resizable: true, width: '30%', displayName: 'Definition Group', enableCellEdit: false, cellTemplate: '<span ng-bind-html="COL_FIELD"></span>' };
            var editField = { field: 'id', width: '100px', displayName: 'Edit', enableCellEdit: false, cellTemplate: '<span class="glyphicon glyphicon-edit" ng-click="edit(COL_FIELD)">-</span>' };

            var scope = {
                q: "",
                totalHits: 0,
                entities: <app.model.EntityListResource[]> null,
                gridOptions: <ngGrid.IGridOptions>{
                    data: 'entities',
                    enableCellSelection: false,
                    enableRowSelection: false,
                    enableHighlighting: false,
                    enableCellEditOnFocus: false,
                    columnDefs: [textField, groupField, editField],
                    rowHeight: rowHeight,
                    enablePaging: true,
                    showFooter: true,
                    totalServerItems: 'totalHits',
                    pagingOptions: {
                        pageSizes: [20, 50, 200],
                        pageSize: 20,
                        currentPage: 1
                    }

                    // rowTemplate: '<div ng-repeat="col in renderedColumns" ng-class="col.colIndex()" class="ngCell { { col.cellClass } }"><div class="ngVerticalBar" ng-style=" { height: rowHeight }" ng-class=" { ngVerticalBarVisible: !$last }">&nbsp;</div><div ng-cell></div></div>'
                },
                edit: (id) => {
                    app.redirectToUrl("entityedit" + '/' + id);
                },
                cancel: () => {
                    app.redirectToRoute(app.Routes.home.url);
                }
            };
            scope = $.extend($scope, scope);

            app.fn.spinStart();
            
            var loadGrid = () => {
                entityService.list(scope.q, scope.gridOptions.pagingOptions.currentPage, scope.gridOptions.pagingOptions.pageSize)
                    .then((x: any) => {
                        scope.entities = x.data.hits;
                        scope.totalHits = x.data.totalHits;
                        app.fn.spinStop();
                    });
            }

            $scope.$watch("q", $debounce(300, (newVal, oldVal) => {
                if (newVal !== oldVal)
                    scope.gridOptions.pagingOptions.currentPage = 1;
                    loadGrid();
            }), true);

            $scope.$watch("gridOptions.pagingOptions", (newVal, oldVal) => {
                if (newVal !== oldVal)
                    loadGrid();
            }, true);

            loadGrid();

            
        }
    }
}