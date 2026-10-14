(function () {
    var hero = document.getElementById('cbHero');
    if (!hero) return;                       // hero hai hi nahi (data khaali) to kuch mat karo

    var slides = hero.querySelectorAll('.cb-hero-slide');
    var dots = hero.querySelectorAll('.cb-hero-dots span');
    var current = 0;
    var timer = null;

    function show(index) {
        slides[current].classList.remove('active');
        dots[current].classList.remove('active');
        current = (index + slides.length) % slides.length;   // aakhri ke baad wapas pehli
        slides[current].classList.add('active');
        dots[current].classList.add('active');
    }

    function stop() {
        if (timer) { clearInterval(timer); timer = null; }
    }

    function start() {
        if (slides.length < 2) return;         // ek hi slide hai to auto-slide ki zaroorat nahi
        stop();
        timer = setInterval(function () { show(current + 1); }, 5000);
    }

    dots.forEach(function (dot, i) {
        dot.addEventListener('click', function () { show(i); start(); });
    });

    hero.addEventListener('mouseenter', stop);   // mouse upar ho to slide na badle
    hero.addEventListener('mouseleave', start);

    start();

    // City badalne par "Now trending in ..." ka naam update karo
    document.addEventListener('cinego:cityChanged', function (e) {
        var name = e.detail && e.detail.cityName;
        if (!name) return;
        document.querySelectorAll('.hero-city-name').forEach(function (el) {
            el.textContent = name;
        });
    });
})();