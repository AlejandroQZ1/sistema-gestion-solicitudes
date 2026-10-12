// Muestra los mensajes de resultado, validacion o error mediante el dialogo compartido.
const mensajeTareas = document.getElementById('mensajeTareas');
if (mensajeTareas) {
    const dialogoMensaje = new bootstrap.Modal(mensajeTareas);
    dialogoMensaje.show();
}
