/**
 * SIGES - Scripts generales del sistema
 * Implementa comportamiento de plantilla SB Admin 2 (USR3) y utilidades de formularios (SOL2)
 */

document.addEventListener('DOMContentLoaded', function () {
    // 1. Toggle del Sidebar (estilo SB Admin 2 - USR3)
    const sidebarToggle = document.getElementById('sidebarToggle');
    const sidebarToggleTop = document.getElementById('sidebarToggleTop');
    const sidebar = document.querySelector('.sidebar');
    const body = document.querySelector('body');

    function toggleSidebar() {
        if (sidebar) {
            sidebar.classList.toggle('toggled');
        }
    }

    if (sidebarToggle) {
        sidebarToggle.addEventListener('click', function (e) {
            e.preventDefault();
            toggleSidebar();
        });
    }

    if (sidebarToggleTop) {
        sidebarToggleTop.addEventListener('click', function (e) {
            e.preventDefault();
            toggleSidebar();
        });
    }

    // 2. Diálogo modal global para confirmación de eliminación (Requisito Flujo de Pantallas - Pág. 16)
    // "¿Realmente desea eliminar el elemento seleccionado? Si / No"
    const modalConfirmacion = document.getElementById('modalConfirmarEliminar');
    if (modalConfirmacion) {
        modalConfirmacion.addEventListener('show.bs.modal', function (event) {
            const button = event.relatedTarget;
            if (!button) return;

            const id = button.getAttribute('data-id');
            const descripcion = button.getAttribute('data-descripcion');
            const inputId = modalConfirmacion.querySelector('#eliminarRegistroId');
            const descSpan = modalConfirmacion.querySelector('#eliminarRegistroDesc');

            if (inputId && id) {
                inputId.value = id;
            }
            if (descSpan && descripcion) {
                descSpan.textContent = descripcion;
            }
        });
    }

    // 3. Cálculo dinámico de IVA (13%) y Total para Desgloses (SOL2)
    const inputMonto = document.getElementById('desgloseMonto');
    const inputIva = document.getElementById('desgloseIVA');
    const inputTotal = document.getElementById('desgloseTotal');

    if (inputMonto && inputIva && inputTotal) {
        function recalcularTotales() {
            const montoVal = parseFloat(inputMonto.value) || 0;
            const ivaVal = Math.round(montoVal * 0.13 * 100) / 100;
            const totalVal = Math.round((montoVal + ivaVal) * 100) / 100;

            inputIva.value = ivaVal.toFixed(2);
            inputTotal.value = totalVal.toFixed(2);
        }

        inputMonto.addEventListener('input', recalcularTotales);
        inputMonto.addEventListener('change', recalcularTotales);
    }
});
