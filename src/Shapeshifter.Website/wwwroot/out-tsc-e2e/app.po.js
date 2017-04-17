"use strict";
var protractor_1 = require("protractor");
var WwwrootPage = (function () {
    function WwwrootPage() {
    }
    WwwrootPage.prototype.navigateTo = function () {
        return protractor_1.browser.get('/');
    };
    WwwrootPage.prototype.getParagraphText = function () {
        return protractor_1.element(protractor_1.by.css('app-root h1')).getText();
    };
    return WwwrootPage;
}());
exports.WwwrootPage = WwwrootPage;
//# sourceMappingURL=app.po.js.map