// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.


function conDate(dateString) {
    if (!dateString) return null;

    return new Date(dateString).toISOString();
}
function toIsoNoTimezone(dateString) {
    if (!dateString) return null;

    const months = {
        Jan: "01", Feb: "02", Mar: "03", Apr: "04",
        May: "05", Jun: "06", Jul: "07", Aug: "08",
        Sep: "09", Oct: "10", Nov: "11", Dec: "12"
    };

    const parts = dateString.split("-");
    const day = parts[0];
    const monthName = parts[1];
    const year = parts[2];

    const month = months[monthName]; // convert "Sep" → "09"

    return `${year}-${month}-${day}T00:00:00`;
}