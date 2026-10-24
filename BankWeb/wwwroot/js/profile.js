function saveProfile()
{
    //includes pfp
    var data = new FormData(document.getElementById('profileForm'));
    const apiUrl = '/Profile/Save';



    const requestOptions = {
        method: 'POST',
        body: data
    };

    document.getElementById('profileMessage').innerHTML = '';

    fetch(apiUrl, requestOptions)
        .then(response => {
            if (!response.ok)
            {
                throw new Error ('Network response not ok');
            }
            return response.json();
        })

        .then (data => {
            //handle data from API
            const jsonObject = data;
            if (jsonObject.success)
            {
                document.getElementById('profileMessage').innerHTML = '<div class="alert alert-success">' + jsonObject.message + '</div>';

                //clear pw and file box, reload picture only
                document.getElementById('Password').value = '';
                document.getElementById('Picture').value = '';
                document.getElementById('profilePicture').src = '/Profile/ProfilePicture?t=' + Date.now();
            }

            else
            {
                document.getElementById('profileMessage').innerHTML = '<div class="alert alert-danger">' + jsonObject.message + '</div>';
            }
        })

        .catch(error => {
            //handle errors that occurred during the fetch
            console.error('Fetch error: ', error);
            document.getElementById('profileMessage').innerHTML = '<div class="alert alert-danger">Could not reach server. Please try again later. </div>';

        });
}