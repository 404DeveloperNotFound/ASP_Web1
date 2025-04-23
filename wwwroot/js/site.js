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
function showToast(message, isError = false) {
    const toastEl = document.getElementById('toastMessage');
    const toastBody = document.getElementById('toastBody');

    toastBody.innerText = message;

    // Change color for success/error
    toastEl.classList.remove('text-bg-success', 'text-bg-danger');
    toastEl.classList.add(isError ? 'text-bg-danger' : 'text-bg-success');

    const toast = new bootstrap.Toast(toastEl);
    toast.show();
}


let form = document.getElementById("create_form").addEventListener("submit", ValidateCreateForm);
const AddedToCart = () => {
    document.querySelectorAll('.add-to-cart-btn').forEach(btn => {
        btn.addEventListener('click', function (e) {
            e.preventDefault(); // ⛔ stop any default behavior

            const itemId = this.getAttribute('data-id');

            fetch(`/Cart/AddToCart/${itemId}`)
                .then(response => response.json())
                .then(data => {
                    alert(data.message); // ✅ show success message
                })
                .catch(error => {
                    alert("An error occurred while adding the item.");
                    console.error("Error:", error);
                });
        });
    });
};
