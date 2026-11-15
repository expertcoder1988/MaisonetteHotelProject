var $j = jQuery.noConflict(), value = 0;

$j(function () {
    $j("input[name='adultminus']").on("click", function (e) {
        if ($j("#adults").val() > 1) {
            value = parseInt($j("#adults").val(), 10) - 1;
            $j("#adults").val(value);
        }
    });

    $j("input[name='adultplus']").on("click", function (e) {
        if ($j("#adults").val() < 13) {
            value = parseInt($j("#adults").val(), 10) + 1;
            $j("#adults").val(value);
        }
    });

    $j("input[name='childminus']").on("click", function (e) {
        if ($j("#childs").val() > 0) {
            value = parseInt($j("#childs").val(), 10) - 1;
            $j("#childs").val(value);
        }
    });

    $j("input[name='childplus']").on("click", function (e) {
        if ($j("#childs").val() < 5) {
            value = parseInt($j("#childs").val(), 10) + 1;
            $j("#childs").val(value);
        }
    });
});

document.addEventListener("DOMContentLoaded", function () {
    var bookNowButton = document.getElementById("bookNowButton"),
        datesInput = document.getElementById("dates");

    if (bookNowButton && datesInput) {
        bookNowButton.addEventListener("click", function (event) {
            event.preventDefault();
            datesInput.focus();
        });
    }

    let heroVideo = document.getElementById("hero-video");
    if (heroVideo) {
        new IntersectionObserver((entries, observer) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    heroVideo.setAttribute("data-jarallax-video", "mp4:../content/paradise/video/home.mp4,webm:../content/paradise/video/home.webm");
                    observer.unobserve(entry.target);
                }
            });
        }).observe(heroVideo);
    }

    let videoHome = document.getElementById("video_home");
    if (videoHome) {
        let observer = new IntersectionObserver((entries, observer) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    videoHome.innerHTML = `
                        <source src="../content/paradise/img/assets/videos/video.mp4" type="video/mp4">
                        <source src="../content/paradise/img/assets/videos/video.webm" type="video/webm">
                    `;
                    videoHome.load();
                    observer.unobserve(entry.target);
                }
            });
        });
        observer.observe(videoHome);
    }
});
