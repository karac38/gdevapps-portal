var StudentReports = (function () {
    var table;
    function init() {
        intiDdls();
    }


    function intiDdls() {
        $('#ddlGradebooks').on("change", function (item) {
            var url = "/Teacher/GetGradebookStudents";
            var id = $(this).val(); // Use $(this) so you don't traverse the DOM again
            if (id === "-1") {//ignore the first element selection
                event.preventDefault();
                return;
            }

            var data = { mainGradeBookId: id }
            var $loader = $('#loader');
            $loader.removeClass("hidden");
            $.ajax({
                method: "POST",
                url: url,
                data: data,
                headers: {
                    RequestVerificationToken: $('input[name="__RequestVerificationToken"').val()
                }
            })
                .done(function (response) {
                    $loader.addClass("hidden");
                    var $ddlStudents = $('#ddlStudents');
                    $ddlStudents.empty();
                    //set the first element
                    $ddlStudents.append($('<option></option>').text("Select class").val("-1"));
                    $.each(response, function (index, item) {
                        if (item && item.id && item.name) {// TODO: Change this is test
                            $ddlStudents.append($('<option></option>').text(item.name).val(item.id));
                        }
                    });

                    var hasValue = !!$('#ddlStudents option').filter(function () { return !this.disabled; }).length;
                    if (hasValue) {
                        $('#divStudents').show('slide', { direction: 'left' });
                        gradeBookId = $ddlStudents.val();
                    } else {
                        $('#divStudents').hide('slide', { direction: 'left' });
                    }
                })
                .fail(function (msg) {
                    $loader.addClass("hidden");
                    console.log("Error occurred while retrieving the Students: " + msg.responseText);
                });

        });

        $("#ddlStudents").on("change", function (event) {
            var url = "/Teacher/GetReport"; //Test
            var id = $(this).val(); // Use $(this) so you don't traverse the DOM again
            if (id === "-1") {//ignore the first element selection
                event.preventDefault();
                return;
            }
            var gradebookId = $("#ddlGradebooks").val();
            var data = { mainGradeBookId: gradebookId, studentEmail: id };
            var $loader = $('#loader');
            $loader.removeClass("hidden");
            $.ajax({
                method: "POST",
                url: url,
                data: data,
                headers: {
                    RequestVerificationToken: $('input[name="__RequestVerificationToken"').val()
                }
            }).done(function (result) {
                $loader.addClass("hidden");
                $('#dvReportResults').html(result);
                initDdlPartial();
                initA4();
            }).fail(function (err) {
                $loader.addClass("hidden");
                console.log(err);
            })
        });


    }

    function initDdlPartial() {
        $('#ddlGrades').on('change', function (event) {
            var id = $(this).val();
            if (id == 1) {
                $('.letter-grade').addClass('hidden');
                $('.final-grade').removeClass('hidden');
            } else if (id == 2) {
                $('.final-grade').addClass('hidden');
                $('.letter-grade').removeClass('hidden');
            }
        });
    }

    var max_pages = 100;
    var page_count = 0;
    //Not for tables
    function snipMe() {
        page_count++;
        if (page_count > max_pages) {
            return;
        }
        var long = $(this)[0].scrollHeight - Math.ceil($(this).innerHeight());
        var children = $(this).children().toArray();
        var removed = [];
        while (long > 0 && children.length > 0) {
            var child = children.pop();
            $(child).detach();
            removed.unshift(child);
            long = $(this)[0].scrollHeight - Math.ceil($(this).innerHeight());
        }
        if (removed.length > 0) {
            var a4 = $('<div class="A4"></div>');
            a4.append(removed);
            $(this).after(a4);
            snipMe.call(a4[0]);
        }
    }

    function initA4(){
        $('.A4').removeClass('hidden');
        // $('.A4').each(function () {
        //     ParentStudents.snipMe.call(this);
        // });
    }

    return {
        init: init,
        snipMe: snipMe
    }
})(jQuery)