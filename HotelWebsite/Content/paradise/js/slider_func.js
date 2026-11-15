(function ($) {
    "use strict";

    $(window).on('load', function () {
        'use strict';

        // Initialize Flexslider on #slider (Main Slider)
        $('#slider').flexslider({
            animation: "fade",        // Animation type
            controlNav: false,          // Disable control navigation (next/prev)
            animationLoop: true,        // Loop through slides
            slideshow: true,            // Enable autoplay
            itemMargin: 40,             // Margin between items
            move: 1                     // Move one slide at a time
        });

        // Initialize Flexslider for the carousel
        $('#carousel_slider').flexslider({
            animation: "slide",         // Animation type
            controlNav: false,          // Disable control navigation (next/prev)
            animationLoop: true,        // Loop through slides
            slideshow: true,            // Enable autoplay
            itemMargin: 40,             // Margin between items
            move: 1                     // Move one slide at a time
        });

    });

})(window.jQuery);
