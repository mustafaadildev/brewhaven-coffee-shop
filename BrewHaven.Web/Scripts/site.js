/* ============================================================
   Brew Haven · site.js — small progressive enhancements
   ============================================================ */
(function () {
    'use strict';

    // ----- Mobile navigation toggle -----
    var toggle = document.getElementById('navToggle');
    if (toggle) {
        toggle.addEventListener('click', function () {
            var open = document.body.classList.toggle('nav-open');
            toggle.setAttribute('aria-expanded', open ? 'true' : 'false');
        });
    }

    // Close the mobile menu after tapping a link
    var nav = document.getElementById('primaryNav');
    if (nav) {
        nav.addEventListener('click', function (e) {
            if (e.target && e.target.tagName === 'A') {
                document.body.classList.remove('nav-open');
                if (toggle) { toggle.setAttribute('aria-expanded', 'false'); }
            }
        });
    }

    // ----- Header shadow on scroll -----
    var header = document.getElementById('siteHeader');
    function onScroll() {
        if (!header) { return; }
        if (window.scrollY > 8) {
            header.classList.add('scrolled');
        } else {
            header.classList.remove('scrolled');
        }
    }
    window.addEventListener('scroll', onScroll, { passive: true });
    onScroll();

    // ----- Reveal-on-scroll animations -----
    var reveals = document.querySelectorAll('.reveal');
    var i;
    if ('IntersectionObserver' in window) {
        var io = new IntersectionObserver(function (entries) {
            for (var j = 0; j < entries.length; j++) {
                if (entries[j].isIntersecting) {
                    entries[j].target.classList.add('in');
                    io.unobserve(entries[j].target);
                }
            }
        }, { threshold: 0.12, rootMargin: '0px 0px -40px 0px' });
        for (i = 0; i < reveals.length; i++) {
            io.observe(reveals[i]);
        }
    } else {
        for (i = 0; i < reveals.length; i++) {
            reveals[i].classList.add('in');
        }
    }

    // ----- Footer year -----
    var year = document.getElementById('year');
    if (year) {
        year.textContent = String(new Date().getFullYear());
    }
})();
