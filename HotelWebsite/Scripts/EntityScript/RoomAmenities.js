$(document).ready(function () {
    if ($('#AvoidReload').val() != "True") {
        // alert("g");
        LoadCategories();
        LoadAmenities();
        //loadCountries();
    }

    function LoadAmenities() {
        $('#RoomIcons').empty().append('<option selected="selected" value="0">Please select</option>');
        $('#RoomIcons').append($('<option></option>').attr('value', "customicon-double-bed").html('<i class="customicon-double-bed"></i>Bed'));
        $('#RoomIcons').append($('<option></option>').attr('value', "customicon-hairdryer").html('<i class="customicon-hairdryer"></i>Washroom'));
        $('#RoomIcons').append($('<option></option>').attr('value', "customicon-television").html('<i class="customicon-television"></i>Lounge'));

    }

    function loadRooms() {
        try {
            var propertyID = new Object();
            propertyID = $('#ddlProperty').val();
              
            $.ajax({
                url: '/Property/LoadpropertyCategories',
                type: 'GET',
                dataType: 'json',
                contentType: 'application/json',
                data: 'propertyID=' + propertyID,
                success: function (data) {
                    $('#rooms').empty().append('<option selected="selected" value="0">Please select</option>');
                    if (data.length > 0) {

                        $.each(data, function (i, item) {
                            $('#rooms').append($('<option></option>').attr('value', item.CateId).text(item.Category));
                        });
                        
                    }
                },
                error: function (err) {
                   alert(err.status + " : " + err.statusText);
                   
                }

            });
        } catch (e) {
            alert(e.status + " : " + e.statusText);
            window.location.href = '/Property//RoomImages';
        }
    }

    function loadRoomPictures() {
        try {
            var RoomCode = new Object();
            RoomCode = $('#rooms').val();
            //  alert(propertyID);
            $.ajax({
                url: '/Property/RoomAmenities',
                type: 'GET',
                dataType: 'json',
                contentType: 'application/json',
                data: 'RoomCode=' + RoomCode,
                success: function (data) {
                    //console.log(data);
                    $("#images").empty();
                    var trHTML = '';
                    trHTML = '<tr><th>S No.</th><th>Icon</th><th>Description</th><th></th><th></th>';
                    var base_url = window.location.origin;
                    if (data.length > 0) {

                        $.each(data, function (i, item) {
                            var desc = item.Description;
                           // console.log(desc);
                            if (desc == null) {
                                desc = "";
                            }

                            trHTML += '<tr><td>' + item.PicNum + '</td><td id=pic>' + item.Icon + '</td>' +
                                "<td><input type='text' id='" + item.AmenityText + "' alt='test' ></td >" +
                                "<td><input type='button' ID='" + item.Id + "' class='Delete btn btn-danger' value='Delete' /></td>";

                        });
                        $('#images').append(trHTML);
                    }
                },
                error: function (err) {
                    alert(err.status + " : " + err.statusText);
                }

            });
        } catch (e) {
            alert(e.status + " : " + e.statusText);
            window.location.href = '/Property//RoomImages';
        }
    }

    $("table").on("click", ".Delete", function (ev) {
        // ev.preventDefault();
        var id = this.id;
        if (confirm("Are you sure to delete this?")) {
            DeleteAmenities(id);
        }
        else {
            return false;
        }

    });

    function DeleteAmenities(id) {

        debugger
        try {
            $.ajax({
                url: '/Property/DeleteAmenities',
                type: 'POST',
                dataType: 'json',
                data: 'ID=' + id,
                //contentType: 'application/json',
                success: function (data) {
                    alert("Amenity removed successfully.");
                    loadRoomPictures();
                },
                error: function (err) {
                    loadRoomPictures();
                    //  alert(err.status + " : " + err.statusText);
                }

            });
        } catch (e) {
            window.location.href = '/Property/Rooms';
        }
    }

    $('#ddlProperty').change(function () {
        loadRooms();
    });

    $(function () {
        try {
            $.ajax({
                url: '/Property/Properties',
                type: 'GET',
                dataType: 'json',
                contentType: 'application/json',
                success: function (data) {
                    // console.log(data);
                    $('#AvoidReload').val("True");
                    //  alert("f");
                    //var ddlPropertyTypes = $("[id*=PropertyTypes]");
                    $('#ddlProperty').empty().append('<option selected="selected" value="0">Please select</option>');
                    //if (data.length > 0) {

                    $.each(data, function (i, item) {
                        $('#ddlProperty').append($('<option></option>').attr('value', item.Id).text(item.PropertyName));
                    });
                    //document.getElementById('#PropertyTypes').selectedIndex = 1;
                    //}
                },
                error: function (err) {
                    //  alert(err.status + " : " + err.statusText);
                }

            });
        } catch (e) {
            window.location.href = '/Property/Index';
        }
    })

    $('#rooms').change(function () {
        loadRoomPictures();
    });

 
    function LoadCategories() {
        try {
            $.ajax({
                url: '/Property/LoadCategories',
                type: 'GET',
                dataType: 'json',
                contentType: 'application/json',
                success: function (data) {

                    $('#RoomCategory').empty().append('<option selected="selected" value="0">Please Select</option>');
                    //if (data.length > 0) {

                    $.each(data, function (i, item) {
                        $('#RoomCategory').append($('<option></option>').attr('value', item.CateId).text(item.Category));
                    });
                    //document.getElementById('#PropertyTypes').selectedIndex = 1;
                    //}
                },
                error: function (err) {
                    //  alert(err.status + " : " + err.statusText);
                }

            });
        } catch (e) {
            window.location.href = '/Property/Index';
        }
    }

    $('#doaddnew').click(function () {
        try {
            //alert(PaxFName);
            if ($("#ddlProperty")[0].selectedIndex == 0) {
                alert("Please select a Property");
                return;
            }
            var propertyID = new Object();
            propertyID = $('#ddlProperty').val();
            $.ajax({
                url: '/Property/AddRoom',
                type: 'POST',
                dataType: 'json',
                data: 'propertyID=' + propertyID,
                //contentType: 'application/json',
                success: function () {
                    alert("New Room Add Succesfully");
                    window.location.href = '/Property//RoomImages';
                },
                error: function (err) {
                    //   alert(err.status + " : " + err.statusText);
                    window.location.href = '/Property//RoomImages';
                },
            });
        } catch (e) {
            // alert(e);
            window.location.href = '/Property//RoomImages';

        }
    })

    function LoadData(id) {

        try {
            $.ajax({
                url: '/Property/loadRoomDetails/',
                type: 'GET',
                dataType: 'json',
                data: 'RoomCode=' + id,
                contentType: 'application/json',
                success: function (data) {
                    // console.log(data);
                    //if ($('#AvoidReload').val() != "True") {
                    //  alert("g");

                    // }
                    if (data.length > 0) {
                        //console.log(data);

                        $.each(data, function (i, item) {

                            //alert(item.RoomCategory);

                            $('#RoomCode').val(item.RoomCode);

                            $('#RoomDescription').val(item.RoomDescription);
                            $('#RoomCategory').val(item.RoomCategory);
                            $('#BedType').val(item.BedType);
                            $('#CheckIn').val(item.CheckIn);

                            $('#CheckOut').val(item.CheckOut);
                            $('#NoOfRooms').val(item.NoOfRooms);
                            $('#Capacity').val(item.Capacity);
                            $('#Adult').val(item.Adult);
                            $('#NoOfChild').val(item.NoOfChild);
                            $("#ShortCode").val(item.ShortCode);
                            $('#MinimumPrice').val(item.MinimumPrice);

                            $('#MinimumStay').val(item.MinimumStay);
                            $('#MaximumStay').val(item.MaximumStay);
                            $('#Size').val(item.Size);

                        });

                    }
                },
                error: function (err) {
                    //  alert(err.status + " : " + err.statusText);
                }

            });
        } catch (e) {
            window.location.href = '/Property/Index';
        }
    }

  

    //document.querySelector('#roomPic').addEventListener("change", previewImages);

   

    $('#AmenityData').click(function () {
       // LoadPicNum();

        // Checking whether FormData is available in browser  
        if (window.FormData !== undefined) {

            // Create FormData object  
            var fileData = new FormData();
            var fileName = "";      
         //   alert(idx);
           
            // Adding one more key to FormData object  
            fileData.append('AmenityName', $('#AmenityName').val());
            fileData.append('RoomIcons', $('#RoomIcons').val());
            fileData.append('CategoryId', $('#rooms').val());
           

            //fileData.append("-", + $('#propertyID').val());
            //SaveData(fileName);
            $.ajax({
                url: '/Property/UploadAmenities',
                type: "POST",
                contentType: false, // Not to set any content header  
                processData: false, // Not to process data  
                data: fileData,
                success: function (result) {
                    alert(result);
                    loadRoomPictures();
                },
                error: function (err) {
                    alert(err.statusText);
                }
            });
        } else {
            alert("FormData is not supported.");
        }
    })


});




