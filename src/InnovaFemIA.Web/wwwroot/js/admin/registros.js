function verStackTrace(trace) {
    document.getElementById('trace-content').innerText = trace;
    document.getElementById('modal-trace').classList.remove('hidden');
}

function cerrarModalTrace() {
    document.getElementById('modal-trace').classList.add('hidden');
}
