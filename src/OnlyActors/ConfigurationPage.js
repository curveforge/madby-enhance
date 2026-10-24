define(['baseView', 'loading', 'emby-button', 'emby-checkbox', 'emby-scroller'], function (BaseView, loading) {
    'use strict';

    var pluginId = '7c2f4d31-9b6e-4a52-8f0d-1e5c3a9b7d24';

    function setChk(page, sel, val) {
        var el = page.querySelector(sel);
        if (el) el.checked = val !== false;
    }

    function val(form, sel) {
        var el = form.querySelector(sel);
        return el ? el.checked : true;
    }

    function bind(page, config) {
        setChk(page, '.chkOnlyActors', config.OnlyActors);
        setChk(page, '.chkHideExternalUrls', config.HideExternalUrls);
        setChk(page, '.chkHideStudios', config.HideStudios);
        setChk(page, '.chkHideTags', config.HideTags);
        setChk(page, '.chkHideSimilar', config.HideSimilar);
        loading.hide();
    }

    function onSubmit(e) {
        e.preventDefault();
        loading.show();
        var form = this;
        ApiClient.getPluginConfiguration(pluginId).then(function (config) {
            config.OnlyActors = val(form, '.chkOnlyActors');
            config.HideExternalUrls = val(form, '.chkHideExternalUrls');
            config.HideStudios = val(form, '.chkHideStudios');
            config.HideTags = val(form, '.chkHideTags');
            config.HideSimilar = val(form, '.chkHideSimilar');
            ApiClient.updatePluginConfiguration(pluginId, config).then(function (result) {
                Dashboard.processPluginConfigurationUpdateResult(result);
                loading.hide();
            });
        });
        return false;
    }

    function saveSilently(form) {
        return ApiClient.getPluginConfiguration(pluginId).then(function (config) {
            config.OnlyActors = val(form, '.chkOnlyActors');
            config.HideExternalUrls = val(form, '.chkHideExternalUrls');
            config.HideStudios = val(form, '.chkHideStudios');
            config.HideTags = val(form, '.chkHideTags');
            config.HideSimilar = val(form, '.chkHideSimilar');
            return ApiClient.updatePluginConfiguration(pluginId, config);
        });
    }

    function View(view, params) {
        BaseView.apply(this, arguments);
        var form = view.querySelector('form');
        if (!form) return;
        form.addEventListener('submit', onSubmit);
        // 勾选即保存（有些客户端没有可见的保存按钮）
        form.addEventListener('change', function () {
            saveSilently(form).then(function () {
                loading.hide();
            }, function () {
                loading.hide();
            });
        });
    }

    Object.assign(View.prototype, BaseView.prototype);

    View.prototype.onResume = function (options) {
        BaseView.prototype.onResume.apply(this, arguments);
        loading.show();
        var page = this.view;
        ApiClient.getPluginConfiguration(pluginId).then(function (response) {
            bind(page, response);
        });
    };

    return View;
});
