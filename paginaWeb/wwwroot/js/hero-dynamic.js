/**
 * Hero Dinámico — Portal Noticias
 * Controlador interactivo para el carrusel de noticias en portada:
 * - Rotación automática con temporizador visual y barra de progreso.
 * - Pausa inteligente en hover, foco o mediante botón de control accesible.
 * - Navegación por flechas, pestañas/tabs interactivas, teclado y gestos táctiles (swipe).
 */
(function () {
    'use strict';

    var AUTOPLAY_DELAY = 6000; // 6 segundos por diapositiva

    function initHeroSlider(root) {
        if (!root) return;

        var slides = Array.prototype.slice.call(root.querySelectorAll('.hero-dynamic__slide'));
        var tabs = Array.prototype.slice.call(root.querySelectorAll('.hero-dynamic__tab'));
        var timerBar = root.querySelector('[data-timer-bar]');
        var counterCurr = root.querySelector('[data-slide-current]');
        var playPauseBtn = root.querySelector('[data-action="toggle-play"]');
        var prevBtn = root.querySelector('[data-action="prev"]');
        var nextBtn = root.querySelector('[data-action="next"]');

        var total = slides.length;
        if (total <= 1) return;

        var currentIndex = 0;
        var timerId = null;
        var isPlaying = true;
        var isHovered = false;
        var isFocused = false;
        var startTime = 0;
        var remainingTime = AUTOPLAY_DELAY;

        function pad(n) {
            return n < 10 ? '0' + n : String(n);
        }

        function restartProgressBar(duration) {
            if (!timerBar) return;
            timerBar.style.transition = 'none';
            timerBar.style.width = '0%';
            // Forzar reflow para reiniciar la animación CSS
            void timerBar.offsetWidth;
            timerBar.style.transition = 'width ' + duration + 'ms linear';
            timerBar.style.width = '100%';
        }

        function pauseProgressBar() {
            if (!timerBar) return;
            var computedWidth = window.getComputedStyle(timerBar).width;
            timerBar.style.transition = 'none';
            timerBar.style.width = computedWidth;
        }

        function goToSlide(index, restartTimer) {
            if (index < 0) index = total - 1;
            if (index >= total) index = 0;

            currentIndex = index;

            // Actualizar diapositivas
            slides.forEach(function (slide, i) {
                var active = i === currentIndex;
                slide.classList.toggle('is-active', active);
                slide.setAttribute('aria-hidden', active ? 'false' : 'true');
            });

            // Actualizar pestañas interactivas
            tabs.forEach(function (tab, i) {
                var active = i === currentIndex;
                tab.classList.toggle('is-active', active);
                tab.setAttribute('aria-selected', active ? 'true' : 'false');
                tab.setAttribute('tabindex', active ? '0' : '-1');
            });

            // Actualizar contador
            if (counterCurr) {
                counterCurr.textContent = pad(currentIndex + 1);
            }

            if (restartTimer !== false && isPlaying && !isHovered && !isFocused) {
                startAutoplay(AUTOPLAY_DELAY);
            }
        }

        function nextSlide() {
            goToSlide(currentIndex + 1);
        }

        function prevSlide() {
            goToSlide(currentIndex - 1);
        }

        function startAutoplay(delay) {
            clearTimeout(timerId);
            var d = delay || AUTOPLAY_DELAY;
            startTime = Date.now();
            remainingTime = d;
            restartProgressBar(d);

            timerId = setTimeout(function () {
                nextSlide();
            }, d);
        }

        function stopAutoplay() {
            clearTimeout(timerId);
            timerId = null;
            pauseProgressBar();
            var elapsed = Date.now() - startTime;
            remainingTime = Math.max(0, remainingTime - elapsed);
        }

        function updatePlayPauseState() {
            if (!playPauseBtn) return;
            var pauseIcon = playPauseBtn.querySelector('.icon-pause');
            var playIcon = playPauseBtn.querySelector('.icon-play');

            if (isPlaying) {
                playPauseBtn.setAttribute('aria-label', 'Pausar rotación automática');
                playPauseBtn.setAttribute('title', 'Pausar rotación automática');
                if (pauseIcon) pauseIcon.style.display = '';
                if (playIcon) playIcon.style.display = 'none';
            } else {
                playPauseBtn.setAttribute('aria-label', 'Reanudar rotación automática');
                playPauseBtn.setAttribute('title', 'Reanudar rotación automática');
                if (pauseIcon) pauseIcon.style.display = 'none';
                if (playIcon) playIcon.style.display = '';
            }
        }

        // Eventos de controles
        if (nextBtn) {
            nextBtn.addEventListener('click', function () {
                nextSlide();
            });
        }

        if (prevBtn) {
            prevBtn.addEventListener('click', function () {
                prevSlide();
            });
        }

        if (playPauseBtn) {
            playPauseBtn.addEventListener('click', function () {
                isPlaying = !isPlaying;
                updatePlayPauseState();
                if (isPlaying) {
                    startAutoplay(remainingTime > 500 ? remainingTime : AUTOPLAY_DELAY);
                } else {
                    stopAutoplay();
                }
            });
        }

        // Clics en tabs
        tabs.forEach(function (tab) {
            tab.addEventListener('click', function () {
                var targetIdx = parseInt(tab.getAttribute('data-tab-index'), 10);
                if (!isNaN(targetIdx) && targetIdx !== currentIndex) {
                    goToSlide(targetIdx);
                }
            });
        });

        // Pausa en hover y foco
        root.addEventListener('mouseenter', function () {
            isHovered = true;
            if (isPlaying) stopAutoplay();
        });

        root.addEventListener('mouseleave', function () {
            isHovered = false;
            if (isPlaying && !isFocused) {
                startAutoplay(remainingTime > 500 ? remainingTime : AUTOPLAY_DELAY);
            }
        });

        root.addEventListener('focusin', function () {
            isFocused = true;
            if (isPlaying) stopAutoplay();
        });

        root.addEventListener('focusout', function (e) {
            if (!root.contains(e.relatedTarget)) {
                isFocused = false;
                if (isPlaying && !isHovered) {
                    startAutoplay(remainingTime > 500 ? remainingTime : AUTOPLAY_DELAY);
                }
            }
        });

        // Navegación por teclado
        root.addEventListener('keydown', function (e) {
            if (e.key === 'ArrowRight') {
                e.preventDefault();
                nextSlide();
            } else if (e.key === 'ArrowLeft') {
                e.preventDefault();
                prevSlide();
            }
        });

        // Gestos táctiles (Swipe)
        var touchStartX = 0;
        var touchStartY = 0;
        var SWIPE_THRESHOLD = 40;

        root.addEventListener('touchstart', function (e) {
            if (e.touches && e.touches[0]) {
                touchStartX = e.touches[0].clientX;
                touchStartY = e.touches[0].clientY;
            }
        }, { passive: true });

        root.addEventListener('touchend', function (e) {
            if (!e.changedTouches || !e.changedTouches[0]) return;
            var diffX = e.changedTouches[0].clientX - touchStartX;
            var diffY = e.changedTouches[0].clientY - touchStartY;

            // Asegurarse de que el gesto fue principalmente horizontal
            if (Math.abs(diffX) > Math.abs(diffY) && Math.abs(diffX) > SWIPE_THRESHOLD) {
                if (diffX < 0) {
                    nextSlide(); // Deslizar hacia la izquierda -> siguiente
                } else {
                    prevSlide(); // Deslizar hacia la derecha -> anterior
                }
            }
        }, { passive: true });

        // Iniciar
        goToSlide(0, false);
        startAutoplay(AUTOPLAY_DELAY);
    }

    // Inicialización al cargar el DOM
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function () {
            var slider = document.querySelector('[data-hero-slider]');
            if (slider) initHeroSlider(slider);
        });
    } else {
        var slider = document.querySelector('[data-hero-slider]');
        if (slider) initHeroSlider(slider);
    }
})();
