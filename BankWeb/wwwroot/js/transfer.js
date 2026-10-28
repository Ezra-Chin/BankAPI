// transfer money page
// load the transfer form into the page
function loadTransferForm() {
    // the account picked on the my accounts page (can be empty)
    var from = document.getElementById('startFrom').value;
    var apiUrl = '/Transfer/Form?from=' + from;

    fetch(apiUrl)
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.text();
        })
        .then(data => {
            // put the form into the empty box
            document.getElementById('transferArea').innerHTML = data;
        })
        .catch(error => {
            // could not reach the server, show an error
            console.error('Fetch error:', error);
            document.getElementById('transferMessage').innerHTML =
                '<div class="alert alert-danger">Could not load the transfer form. Please try again.</div>';
        });
}

function performTransfer() {
    // read what the user typed
    var amount = document.getElementById('Amount').value;
    var data = {
        FromAccountNumber: document.getElementById('FromAccountNumber').value,
        ToAccountNumber: document.getElementById('ToAccountNumber').value,
        Amount: (amount === '') ? null : Number(amount),
        Description: document.getElementById('Description').value
    };
    const apiUrl = '/Transfer/Send';

    const headers = {
        'Content-Type': 'application/json', 
        'RequestVerificationToken': document.getElementById('requestToken').value // Security token (anti-forgery)
    };

    // POST with the data turned into a json string
    const requestOptions = {
        method: 'POST',
        headers: headers,
        body: JSON.stringify(data) 
    };

    // hide the button while sending so  double click cant send the money twice
    document.getElementById('transferButton').style.display = "none";
    document.getElementById('transferMessage').innerHTML = '';

    fetch(apiUrl, requestOptions)
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.text();
        })
        .then(data => {
            document.getElementById('transferArea').innerHTML = data;
        })
        .catch(error => {
            console.error('Fetch error:', error);
            document.getElementById('transferButton').style.display = "inline-block";
            document.getElementById('transferMessage').innerHTML =
                '<div class="alert alert-danger">Could not reach the server. Please try again.</div>';
        });
}

// load the form when the page has loaded
document.addEventListener("DOMContentLoaded", loadTransferForm);