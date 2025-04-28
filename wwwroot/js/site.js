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
document.addEventListener('DOMContentLoaded', function () {
    toastr.options = {
        "closeButton": true,
        "debug": false,
        "newestOnTop": false,
        "progressBar": true,
        "positionClass": "toast-bottom-right",
        "preventDuplicates": false,
        "onclick": null,
        "showDuration": "300",
        "hideDuration": "1000",
        "timeOut": "3000",
        "extendedTimeOut": "1000",
        "showEasing": "swing",
        "hideEasing": "linear",
        "showMethod": "fadeIn",
        "hideMethod": "fadeOut"
        
    };

    // Add event listener to add-to-cart buttons
    document.querySelectorAll('.add-to-cart-btn').forEach(btn => {
        btn.addEventListener('click', function () {
            const itemId = this.getAttribute('data-id');

            fetch(`/Cart/AddToCart/${itemId}`)
                .then(response => response.json())
                .then(data => {
                    showToast(data.message, data.isError);
                })
                .catch(error => {
                    showToast("An error occurred while adding the item.", true);
                    console.error("Error:", error);
                });
        });
    });
});

// showToast function for showing notifications
function showToast(message, isError = false) {
    if (isError) {
        toastr.error(message);
    } else {
        toastr.success(message);
    }
}


let form = document.getElementById("create_form").addEventListener("submit", ValidateCreateForm);
