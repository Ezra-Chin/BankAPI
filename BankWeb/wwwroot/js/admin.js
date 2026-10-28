
//keep track of which table is being shown
var currentTable = '';

//all the given filters 
var search = '';
var type = '';
var from = '';
var to = '';
var page = 1;
var sortBy = 'date';
var sortDir = 'desc';

//audit table page
var auditPage = 1;


//load the html from the controller and put it inside the div (This is mainfly for partial views )
function loadIntoDiv(url, divId)

{
    //calls the api 
    fetch(url)
        .then(response => {
            if (!response.ok) {
                throw new Error('Failed to load table');
            }
            return response.text();
        })
        .then(data => {
            document.getElementById(divId).innerHTML = data;
        })
        .catch(error => {
            console.error(error);
            document.getElementById(divId).innerHTML =
                '<div class="alert alert-danger">Could not load table.</div>';
        });
}


//load transaction table w given filters
function loadTransactions() {
    var url = '/Admin/TransactionsTable'
        + '?search=' + encodeURIComponent(search)
        + '&type=' + encodeURIComponent(type)
        + '&from=' + from
        + '&to=' + to
        + '&page=' + page
        + '&sortBy=' + sortBy
        + '&sortDir=' + sortDir;
        //calls the function above 
    loadIntoDiv(url, 'adminTransactions');
}


//apply the selected filters
function filterTransactions() {
    var selectedFrom = document.getElementById('fromDate').value;
    var selectedTo = document.getElementById('toDate').value;

    //make sure the from date is not later than the to date
    if (selectedFrom !== '' && selectedTo !== '' && selectedFrom > selectedTo) {
        document.getElementById('adminTransactions').innerHTML =
            '<div class="alert alert-danger">From date cannot be after To date.</div>';
        return;
    }

    search = document.getElementById('searchText').value.trim();
    type = document.getElementById('typeFilter').value;
    from = selectedFrom;
    to = selectedTo;

    //start from the first page when filtering
    page = 1;
    loadTransactions();
}


//clear the filters 
function clearTransactionFilter() {
    document.getElementById('searchText').value = '';
    document.getElementById('typeFilter').value = '';
    document.getElementById('fromDate').value = '';
    document.getElementById('toDate').value = '';

    search = '';
    type = '';
    from = '';
    to = '';
    page = 1;

    loadTransactions();
}


//sort the transactions table
function sortTransactions(column) {
    //if the same column is clicked, reverse the sort direction
    if (sortBy === column) {
        sortDir = (sortDir === 'desc') ? 'asc' : 'desc';
    } else {
        sortBy = column;
        sortDir = 'desc';
    }

    page = 1;
    loadTransactions();
}


//load the audit table
function loadAudit() {
    loadIntoDiv('/admin/auditTable?page=' + auditPage, 'auditTable');
}


//handle the page buttons in the table
function goToPage(number) {
    if (currentTable === 'audit') {
        auditPage = number;
        loadAudit();
    } else {
        page = number;
        loadTransactions();
    }
}


//load the tables when the page opens
//if is adminTransactions then load the htmlcs for transactions and audit if its auditTable
//it checks the entire page if there's these specific id, if yes , replace the div with the newly fetch PartialDiv
function adminPageLoaded() {
    if (document.getElementById('adminTransactions') != null) {
        currentTable = 'transactions';
        loadTransactions();
    }

    if (document.getElementById('auditTable') != null) {
        currentTable = 'audit';
        loadAudit();
    }
}


//run when the page finishes loading
//auto calls adminPageLoaded for the page 
document.addEventListener('DOMContentLoaded', adminPageLoaded);
