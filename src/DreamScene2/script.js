// @name        bilibili 自动网页全屏
// @author      sangkuanji
// @license MIT

(function () {
    if (location.hostname == 'www.bilibili.com' && /\/video\/BV/i.test(location.pathname)) {
        window.addEventListener('load', function () {
            console.log("load success");
            let elementNames = ["bpx-player-ctrl-web-enter", "bilibili-player-iconfont-web-fullscreen-off", "player_pagefullscreen_player", "squirtle-pagefullscreen-inactive"];
            for (var i = 0; i < elementNames.length; i++) {
                waitElement(elementNames[i]);
            }
        });
    }

    function waitElement(elementName) {
        var _times = 20,
            _interval = 1000,
            _self = document.getElementsByClassName(elementName)[0],
            _iIntervalID;
        if (_self != undefined) {
            _self.click();
        } else {
            _iIntervalID = setInterval(function () {
                if (!_times) {
                    clearInterval(_iIntervalID);
                }
                _times <= 0 || _times--;
                _self = document.getElementsByClassName(elementName)[0];
                if (_self == undefined) {
                    _self = document.getElementById(elementName);
                }
                if (_self != undefined) {
                    _self.click();
                    clearInterval(_iIntervalID);
                }
            }, _interval);
        }
    }
})();