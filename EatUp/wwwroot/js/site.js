// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
document.addEventListener("DOMContentLoaded", function () {

    if (!navigator.geolocation) return;

    navigator.geolocation.getCurrentPosition(
        function (position) {
            fetch('/Account/SaveLocation', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded'
                },
                body:
                    'latitude=' + position.coords.latitude +
                    '&longitude=' + position.coords.longitude
            });
        },
        function (error) {
            console.log("Geolocation denied or unavailable.");
        }
    );
});
