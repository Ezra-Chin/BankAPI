// accounts summary page and transaction history page
function loadAccounts() {
    var apiUrl = '/Accounts/List';

    fetch(apiUrl)
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.text();
        })
        .then(data => {
            // put the cards into the empty box
            document.getElementById('accountList').innerHTML = data;
        })
        .catch(error => {
            // could not reach the server, show an error
            console.error('Fetch error:', error);
            document.getElementById('accountList').innerHTML =
                '<div class="alert alert-danger">Could not load your accounts. Please try again.</div>';
        });
}
// the current filter, page and sort
var fromDate = '';
var toDate = '';
var page = 1;
var sortBy = 'date';
var sortDir = 'desc';

// load the history table using the current filter, page and sort
function loadHistory() {
    var accountNumber = document.getElementById('accountNumber').value;
    var apiUrl = '/Accounts/HistoryTable/' + accountNumber
        + '?from=' + fromDate + '&to=' + toDate
        + '&page=' + page + '&sortBy=' + sortBy + '&sortDir=' + sortDir;

    fetch(apiUrl)
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.text();
        })
        .then(data => {
            // put the table into the empty box
            document.getElementById('historyTable').innerHTML = data;
        })
        .catch(error => {
            // could not reach the server, show an error
            console.error('Fetch error:', error);
            document.getElementById('historyTable').innerHTML =
                '<div class="alert alert-danger">Could not load the transactions. Please try again.</div>';
        });
}

function filterHistory() {
    var from = document.getElementById('fromDate').value;
    var to = document.getElementById('toDate').value;

    // from date cant be after the to date
    if (from !== '' && to !== '' && from > to) {
        document.getElementById('historyTable').innerHTML =
            '<div class="alert alert-danger">The \'From\' date must be on or before the \'To\' date.</div>';
        return;
    }
    fromDate = from;
    toDate = to;
    page = 1;
    loadHistory();
}
function clearFilter() {
    document.getElementById('fromDate').value = '';
    document.getElementById('toDate').value = '';
    fromDate = '';
    toDate = '';
    page = 1;
    loadHistory();
}
function sortHistory(column) {
    if (sortBy === column) {
        sortDir = (sortDir === 'desc') ? 'asc' : 'desc';
    }
    else {
        sortBy = column;
        sortDir = 'desc';
    }
    page = 1;
    loadHistory();
}
// page button clicked
function goToPage(number) {
    page = number;
    loadHistory();
}
// runs when the page has loaded, loads whichever box this page has
function pageLoaded() {
    if (document.getElementById('accountList') != null) {
        loadAccounts();
    }
    if (document.getElementById('historyTable') != null) {
        loadHistory();
    }
}

document.addEventListener("DOMContentLoaded", pageLoaded);