console.log("KCET Prep site loaded");

$(document).ready(function () {
    $('#predictor-form').validate({
        rules: {
            Rank: {
                required: true,
                min: 1
            },
            SelectedCategory: {
                required: true
            }
        },
        messages: {
            Rank: {
                required: "Please enter your rank",
                min: "Rank must be at least 1"
            },
            SelectedCategory: {
                required: "Please select a category"
            }
        },
        submitHandler: function (form) {
            console.log("Form submitted with: Rank=" + $('#Rank').val() + ", Category=" + $('#SelectedCategory').val() + ", Branches=" + $('input[name="SelectedBranches"]:checked').map(function () { return this.value; }).get().join(',') + ", Districts=" + $('input[name="SelectedDistricts"]:checked').map(function () { return this.value; }).get().join(',') + ", Colleges=" + $('input[name="SelectedColleges"]:checked').map(function () { return this.value; }).get().join(','));
            $('#submit-btn').prop('disabled', true);
            $('#submit-text').addClass('hidden');
            $('#spinner').removeClass('hidden');
            form.submit();
        }
    });
});