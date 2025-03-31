// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

const ValidateCreateForm = (e) => {
    const name = document.getElementById("create_name").value;
    const price = document.getElementById("create_price").value;

    if (!name || !price) {
        alert("Invalid Input Data!");
        e.preventDefault();
    }
}

let form = document.getElementById("create_form").addEventListener("submit", ValidateCreateForm);
