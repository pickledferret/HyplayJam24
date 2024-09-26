mergeInto(LibraryManager.library, {
    location_href: function () {
        var str = window.top.location.href;
        var bufferSize = lengthBytesUTF8(str) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(str, buffer, bufferSize);
        return buffer;
    },
    
    InitLogin: function ()
    {
        console.log(window.location.href);

        const cookies = document.cookie.split(';');

        for (let i = 0; i < cookies.length; i++) {
            const cookie = cookies[i].trim();
            if (cookie.startsWith('authToken=')) {
                var found = cookie.substring('authToken='.length);
                console.log("found cookie " + found);
            }
        }
    },
    
    DoLoginRedirect: function (appIdPtr, expiryPtr) {
        var expiry = UTF8ToString(expiryPtr);
        var appId = UTF8ToString(appIdPtr);
    
        // Construct the full HyPlay OAuth URL
        var redirectUri = window.location.href;
        var url = "https://hyplay.com/oauth/authorize/?appId=" + appId + "&chain=HYCHAIN&responseType=token" + expiry + "&redirectUri=" + redirectUri;
        window.top.location = url;
    },
    
    DoLoginPopup: function (appIdPtr, expiryPtr) {
        var expiry = UTF8ToString(expiryPtr);
        var appId = UTF8ToString(appIdPtr);
    
        // Construct the full HyPlay OAuth URL
        var redirectUri = window.location.href.replace("index.html", "redirect.html");
        var url = "https://hyplay.com/oauth/authorize/?appId=" + appId + "&chain=HYCHAIN&responseType=token" + expiry + "&redirectUri=" + redirectUri;
        // Open a new popup window
        const popup = window.open(url, "PopupWindow", "width=600,height=600");
        var intervalId = setInterval(function() {
            try {
                // Check if popup was closed
                if (popup.closed) {
                    //clearInterval(intervalId);
                    //console.error("OAuth popup closed prematurely");
                    //return;
                }
    
                // Check URL for the access token
                var hash = popup.location.hash.substring(1);
                var params = new URLSearchParams(hash);
                var accessToken = params.get('token');
    
                if (accessToken) {
                    console.log('Access token:', accessToken);
                    SendMessage('OAuthManager', 'ReceiveMessage', accessToken);
                    clearInterval(intervalId);
                    popup.close();
                }
            } catch (e) {
                // Handle cross-origin errors (if popup navigates to a different domain)
                if (e instanceof DOMException && e.name === "SecurityError") {
                    console.warn("Cross-origin error accessing popup URL:", e);
                } else {
                    console.error("Error accessing popup URL:", e);
                }
            }
        }, 500); // Check every 500 milliseconds
    }
});