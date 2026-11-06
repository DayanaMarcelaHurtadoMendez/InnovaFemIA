/**
 * InnovaFemIA - Menú de Navegación Responsivo
 * Controla el despliegue del menú móvil
 */
document.addEventListener('DOMContentLoaded', function () {
    var toggleBtn = document.getElementById('nav-mobile-toggle');
    if (toggleBtn) {
        toggleBtn.addEventListener('click', function () {
            var menu = document.getElementById('nav-mobile-menu');
            if (menu) {
                var expanded = this.getAttribute('aria-expanded') === 'true';
                menu.classList.toggle('hidden');
                this.setAttribute('aria-expanded', String(!expanded));
            }
        });
    }
});
