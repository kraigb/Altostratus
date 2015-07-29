function getFragments() {

    var dictionary = {};

    if (window.location.hash) {

        var hash = window.location.hash.substring(1);
        var pairs = hash.split('&');

        for (var i = 0; i < pairs.length; i++) {
            var pair = pairs[i].split('=');

            var key = decodeURIComponent(pair[0]);
            var value = pair[1] ? decodeURIComponent(pair[1]) : '';

            dictionary[key] = value;
        }

    }
    return dictionary;
}


var Auth = {
    _tokenKey: 'accessToken',

    getRequestHeaders: function () {
        var headers = {};
        var token = this.getAccessToken();
        if (token) {
            headers.Authorization = 'Bearer ' + token;
        }
        return headers;
    },

    getAccessToken: function () {
        return sessionStorage.getItem(this._tokenKey);
    },

    setAccessToken: function (token) {
        if (token) {
            sessionStorage.setItem(this._tokenKey, token);
            this.onAuth.notifySubscribers(true, "auth");
        }
        else {
            sessionStorage.removeItem(this._tokenKey);
            this.onAuth.notifySubscribers(false, "auth");
        }
    },

    onAuth: new ko.subscribable()
};


function AuthViewModel() {
    var self = this;

    self.providers = ko.observableArray(); // List of login providers with URLs

    // user info
    self.email = ko.observable();
    self.hasRegistered = ko.observable(false);   
    self.loginProvider = ko.observable();

    self.result = ko.observable(); 

    function showError(jqXHR) {
        self.result(jqXHR.status + ': ' + jqXHR.statusText);
    }

    // Get a list of all the social login providers that the app supports
    self.getProviders = function () {
        $.ajax({
            type: 'GET',
            url: '/api/account/externalLogins?returnUrl=/'
        }).done(function (data) {
            self.providers.removeAll();
            ko.utils.arrayPushAll(self.providers, data);
        }).fail(showError);

    }

    self.login = function (provider) {
        location.href = provider.Url;
    }

    self.getUserInfo = function () {
        $.ajax({
            type: 'GET',
            url: '/api/Account/UserInfo',
            headers: Auth.getRequestHeaders()
        }).done(function (data) {
            self.email(data.Email);
            self.hasRegistered = ko.observable(data.HasRegistered);   

            if (!data.HasRegistered) {
                self.register(data);
            }

        }).fail(showError);;
    }

    self.register = function (userInfo) {
        var providerName = userInfo.LoginProvider;

        var data = {
            Email: userInfo.Email,
        };


        $.ajax({
            type: 'POST',
            url: '/api/Account/RegisterExternal',
            contentType: 'application/json; charset=utf-8',
            headers: Auth.getRequestHeaders(),
            data: JSON.stringify(data)
        }).done(function (data) {

            // The loginProvider tells us which provider was used. Look this up in our list of providers
            var provider = ko.utils.arrayFirst(self.providers(), function (item) {
                return item.Name == providerName;
            });

            // Login using this provider.
            self.login(provider);
        }).fail(showError);;
    }


    self.logout = function() {

        $.ajax({
            type: 'POST',
            url: '/api/Account/Logout',
            headers: Auth.getRequestHeaders()
        });

        Auth.setAccessToken(null);
        self.email(null);
        self.hasRegistered(false);
        self.loginProvider(null);

        // Refresh the provider list
        self.getProviders();
    }



    var f = getFragments();
    var token = f['access_token'];
    if (token) {
        window.location.replace("#");
        console.log(token);
        Auth.setAccessToken(token);
        self.getUserInfo();
    }

    if (Auth.getAccessToken()) {
        // Cache this?
        self.getUserInfo();
    }
    else {
        // Kick off by getting the provider list
        self.getProviders();
    }
}

function UserPrefsViewModel() {
    var self = this;

    self.initialized = ko.observable(false);
    self.working = ko.observable(false);

    // User prefs
    self.ConversationLimit = ko.observable(100);
    self.SortOrder = ko.observable(0);
    self.AllCategories = ko.observableArray();

    // Returns the list of selected category names
    self.SelectedCategories = ko.computed(function () {
        // Get selected categories
        var cats = ko.utils.arrayFilter(self.AllCategories(), function (item) {
            return item.Checked();
        });
        // Flatten to array of strings
        return $.map(cats, function (val) { return val.Name(); });
    });

    function CheckedCategory(name) {
        this.Name = ko.observable(name);
        this.Checked = ko.observable(true);
    }

    function updateSelectedCategories(cats) {
        // Loop through all the categories
        ko.utils.arrayForEach(self.AllCategories(), function (item) {
            var found = ko.utils.arrayFirst(cats, function (c) {
                return item.Name() == c
            });
            item.Checked(found ? true : false);
        });
        self.AllCategories.valueHasMutated();
    }


    self.getPrefs = function () {
        if (self.initialized()) {
            return;
        }

        $.ajax({
            type: 'GET',
            url: '/api/userpreferences',
            headers: Auth.getRequestHeaders()
        }).done(function (data) {
            self.ConversationLimit(data.ConversationLimit);
            self.SortOrder(data.SortOrder);
            updateSelectedCategories(data.Categories);
            self.initialized(true);
        }).error(function (jqXHR) {
            if (jqXHR.status == 404)
            {
                // Expected if there are no user prefs
                self.initialized(true);
            }
        });
    }

    self.setPrefs = function () {
        self.working(true);

        var data = {
            ConversationLimit: self.ConversationLimit(),
            SortOrder: self.SortOrder(),
            Categories: self.SelectedCategories()
        };

        $.ajax({
            type: 'PUT',
            url: '/api/userpreferences',
            headers: Auth.getRequestHeaders(),
            data: JSON.stringify(data),
            contentType: 'application/json; charset=UTF-8'
        }).error(function (jqXHR) {
            alert(jqXHR.statusText);
        }).always(function () {
            self.working(false);
        });
    }

    self.isLoggedIn = ko.observable(false);
    if (Auth.getAccessToken())
    {
        self.isLoggedIn(true);
    }

    Auth.onAuth.subscribe(self.isLoggedIn, null, "auth");


    // Kick off by getting the list of categories
    $.ajax({
        type: 'GET',
        url: '/api/categories'
    }).done(function (data) {
        var c = $.map(data, function(categoryName) {
            return new CheckedCategory(categoryName);
        });
        self.AllCategories(c);
    }).error(function (jqXHR) {
        alert(jqXHR.statusText);
    });;

}

function ConversationsViewModel() {
    var self = this;

    self.conversations = ko.observableArray();

    self.error = ko.observable();

    self.getAll = function () {
        self.conversations.removeAll();
        $.ajax({
            type: 'GET',
            url: '/api/conversations',
            headers: Auth.getRequestHeaders()
        }).done(function (data) {
            ko.utils.arrayPushAll(self.conversations, data);
        }).error(function (jqXHR) {
            self.error(jqXHR.responseText);
        });
    }

    // BUGBUG Right now I don't re-load the conversations after you log in


    self.getAll();
}

ko.applyBindings(new AuthViewModel(), document.getElementById('login-panel'));
ko.applyBindings(new UserPrefsViewModel(), document.getElementById('user-prefs'));
ko.applyBindings(new ConversationsViewModel(), document.getElementById('accordion'));