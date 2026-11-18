/**
 * Gestor de documentos del formulario de artículo.
 * Mantiene sincronizados los checkboxes del modal con los inputs ocultos
 * `DocumentIds` que se envían con el formulario.
 */
(function () {
    'use strict';

    var modal = document.getElementById('documentManagerModal');
    if (!modal || modal.dataset.bound === 'true') return;
    modal.dataset.bound = 'true';

    var listUrl = modal.dataset.listUrl || '/admin/documentos/listado';
    var uploadUrl = modal.dataset.uploadUrl || '/admin/documentos/subir-ajax';
    var deleteUrl = modal.dataset.deleteUrl || '/admin/documentos/eliminar-ajax';
    var currentArticleId = modal.dataset.currentArticleId || '';
    var listElement = document.getElementById('documentManagerList');
    var emptyElement = document.getElementById('documentManagerEmpty');
    var loadingElement = document.getElementById('documentManagerLoading');
    var searchElement = document.getElementById('documentManagerSearch');
    var fileInput = document.getElementById('documentManagerFileInput');
    var uploadButton = document.getElementById('documentManagerUploadButton');
    var uploadLabel = document.getElementById('documentManagerUploadLabel');
    var uploadError = document.getElementById('documentManagerUploadError');
    var summaryElement = document.getElementById('documentManagerSelectionSummary');
    var hiddenContainer = document.getElementById('articleDocumentIds');
    var chipsContainer = document.getElementById('selectedDocumentChips');

    var allDocuments = [];
    var selectedIds = new Set();
    var searchTerm = '';
    var loading = false;

    if (hiddenContainer) {
        Array.from(hiddenContainer.querySelectorAll('input[name="DocumentIds"]'))
            .map(function (input) { return input.value; })
            .filter(Boolean)
            .forEach(function (id) { selectedIds.add(id); });
    }

    function getToken() {
        return modal.querySelector('input[name="__RequestVerificationToken"]')?.value ||
            document.querySelector('input[name="__RequestVerificationToken"]')?.value ||
            '';
    }

    function escapeHtml(value) {
        return String(value ?? '')
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;');
    }

    function documentLabel(document) {
        return document?.displayName || document?.originalFileName || 'Documento';
    }

    function documentIcon(extension) {
        var ext = String(extension || '').toLowerCase();
        if (ext === '.pdf') {
            return '<span class="inline-flex h-10 w-10 items-center justify-center rounded-xl bg-rose-50 text-rose-600 text-[10px] font-black">PDF</span>';
        }
        if (ext === '.doc' || ext === '.docx' || ext === '.odt' || ext === '.rtf') {
            return '<span class="inline-flex h-10 w-10 items-center justify-center rounded-xl bg-blue-50 text-blue-600 text-[10px] font-black">DOC</span>';
        }
        if (ext === '.xls' || ext === '.xlsx' || ext === '.ods' || ext === '.csv') {
            return '<span class="inline-flex h-10 w-10 items-center justify-center rounded-xl bg-emerald-50 text-emerald-600 text-[10px] font-black">XLS</span>';
        }
        if (ext === '.ppt' || ext === '.pptx' || ext === '.odp') {
            return '<span class="inline-flex h-10 w-10 items-center justify-center rounded-xl bg-amber-50 text-amber-600 text-[10px] font-black">PPT</span>';
        }
        return '<span class="inline-flex h-10 w-10 items-center justify-center rounded-xl bg-slate-100 text-slate-500 text-[10px] font-black">DOC</span>';
    }

    function syncHiddenInputs() {
        if (hiddenContainer) {
            hiddenContainer.innerHTML = '';
            selectedIds.forEach(function (id) {
                var input = document.createElement('input');
                input.type = 'hidden';
                input.name = 'DocumentIds';
                input.value = id;
                hiddenContainer.appendChild(input);
            });
        }

        renderChips();
    }

    function renderChips() {
        if (!chipsContainer) return;

        chipsContainer.innerHTML = '';
        if (selectedIds.size === 0) {
            chipsContainer.innerHTML = '<span class="text-[11px] text-slate-400 italic">Sin documentos seleccionados.</span>';
            return;
        }

        selectedIds.forEach(function (id) {
            var document = allDocuments.find(function (item) { return item.id === id; });
            var chip = document.createElement('span');
            chip.className = 'inline-flex max-w-full items-center gap-1.5 rounded-lg border border-slate-200 bg-slate-50 px-2.5 py-1 text-[11px] font-semibold text-slate-700';
            chip.innerHTML = '<span class="truncate max-w-[170px]">' + escapeHtml(documentLabel(document)) +
                '</span><button type="button" class="text-slate-400 hover:text-rose-600" aria-label="Quitar documento">&times;</button>';
            chip.querySelector('button')?.addEventListener('click', function () {
                window.removeArticleDocument(id);
            });
            chipsContainer.appendChild(chip);
        });
    }

    function updateSummary() {
        if (summaryElement) {
            summaryElement.textContent = selectedIds.size +
                (selectedIds.size === 1 ? ' documento seleccionado' : ' documentos seleccionados');
        }
    }

    function render() {
        if (!listElement) return;

        if (loading) {
            loadingElement?.classList.remove('hidden');
            emptyElement?.classList.add('hidden');
            listElement.innerHTML = '';
            return;
        }

        loadingElement?.classList.add('hidden');

        var term = searchTerm.trim().toLowerCase();
        var filtered = allDocuments.filter(function (document) {
            if (!term) return true;
            return String(document.displayName || '').toLowerCase().includes(term) ||
                String(document.originalFileName || '').toLowerCase().includes(term) ||
                String(document.description || '').toLowerCase().includes(term);
        });

        listElement.innerHTML = '';
        emptyElement?.classList.toggle('hidden', filtered.length > 0);

        filtered.forEach(function (document) {
            var selected = selectedIds.has(document.id);
            var row = document.createElement('div');
            row.className = 'flex items-center gap-3 rounded-xl border bg-white p-3 shadow-2xs transition ' +
                (selected ? 'border-[#00509D] ring-1 ring-[#00509D]/30' : 'border-slate-200 hover:border-slate-300');

            var usage = document.isUsed && document.usedInArticleTitle
                ? ' · En: ' + escapeHtml(document.usedInArticleTitle)
                : '';

            row.innerHTML =
                '<label class="flex min-w-0 flex-1 cursor-pointer items-center gap-3">' +
                    '<input type="checkbox" class="h-4 w-4 rounded border-slate-300 text-[#00296B] focus:ring-[#00296B]" ' +
                        (selected ? 'checked' : '') + ' />' +
                    documentIcon(document.extension) +
                    '<span class="min-w-0">' +
                        '<span class="block truncate text-sm font-bold text-slate-800">' +
                            escapeHtml(documentLabel(document)) + '</span>' +
                        '<span class="block truncate text-[11px] text-slate-500">' +
                            escapeHtml(String(document.extension || '').toUpperCase()) + ' · ' +
                            escapeHtml(document.sizeLabel || '') + ' · ' +
                            escapeHtml(document.uploadedAt || '') + usage +
                        '</span>' +
                    '</span>' +
                '</label>' +
                '<div class="flex shrink-0 items-center gap-2">' +
                    '<a href="' + escapeHtml(document.downloadUrl || '#') + '" target="_blank" rel="noopener" ' +
                       'class="rounded-lg border border-slate-300 bg-white px-2.5 py-1 text-[11px] font-semibold text-slate-700 hover:bg-slate-100">Ver</a>' +
                    '<button type="button" class="rounded-lg border border-rose-200 bg-rose-50 px-2.5 py-1 text-[11px] font-semibold text-rose-700 hover:bg-rose-100">Eliminar</button>' +
                '</div>';

            row.querySelector('input[type="checkbox"]')?.addEventListener('change', function () {
                toggleSelection(document.id);
            });
            row.querySelector('button')?.addEventListener('click', function (event) {
                event.preventDefault();
                event.stopPropagation();
                deleteDocument(document.id);
            });

            listElement.appendChild(row);
        });

        updateSummary();
    }

    function toggleSelection(id) {
        if (selectedIds.has(id)) selectedIds.delete(id);
        else selectedIds.add(id);

        syncHiddenInputs();
        render();
    }

    async function loadCatalog() {
        loading = true;
        render();

        try {
            var response = await fetch(listUrl + '?currentArticleId=' + encodeURIComponent(currentArticleId), {
                headers: { Accept: 'application/json' }
            });

            if (!response.ok) throw new Error('No se pudo cargar la biblioteca de documentos.');

            var data = await response.json();
            allDocuments = Array.isArray(data) ? data : [];
            renderChips();
        } catch (error) {
            console.error(error);
            if (uploadError) {
                uploadError.textContent = error.message || 'No se pudo cargar la biblioteca.';
                uploadError.classList.remove('hidden');
            }
        } finally {
            loading = false;
            render();
        }
    }

    async function uploadFile(file) {
        if (!file) return;

        if (uploadError) uploadError.classList.add('hidden');
        if (uploadButton) uploadButton.disabled = true;
        if (uploadLabel) uploadLabel.textContent = 'Subiendo...';

        try {
            var formData = new FormData();
            formData.append('file', file);
            formData.append('displayName', file.name.replace(/\.[^.]+$/, ''));
            formData.append('isPublic', 'false');

            var response = await fetch(uploadUrl, {
                method: 'POST',
                headers: { RequestVerificationToken: getToken() },
                body: formData
            });

            var data = await response.json().catch(function () { return null; });
            if (!response.ok || !data?.success) {
                throw new Error(data?.message || 'No se pudo subir el documento.');
            }

            var uploaded = Object.assign({}, data.document, {
                uploadedAt: 'Ahora',
                isUsed: false,
                usedInArticleTitle: null
            });

            allDocuments.unshift(uploaded);
            selectedIds.add(uploaded.id);
            syncHiddenInputs();
            render();
        } catch (error) {
            if (uploadError) {
                uploadError.textContent = error.message || 'No se pudo subir el documento.';
                uploadError.classList.remove('hidden');
            }
        } finally {
            if (uploadButton) uploadButton.disabled = false;
            if (uploadLabel) uploadLabel.textContent = 'Subir documento';
            if (fileInput) fileInput.value = '';
        }
    }

    async function deleteDocument(id) {
        var document = allDocuments.find(function (item) { return item.id === id; });
        if (!window.confirm('¿Eliminar "' + documentLabel(document) + '"? Esta acción borra el archivo y lo quita de los artículos que lo usen.')) {
            return;
        }

        try {
            var response = await fetch(deleteUrl + '/' + encodeURIComponent(id), {
                method: 'POST',
                headers: { RequestVerificationToken: getToken() }
            });

            if (!response.ok) {
                var data = await response.json().catch(function () { return null; });
                throw new Error(data?.message || 'No se pudo eliminar el documento.');
            }

            allDocuments = allDocuments.filter(function (item) { return item.id !== id; });
            selectedIds.delete(id);
            syncHiddenInputs();
            render();
        } catch (error) {
            if (uploadError) {
                uploadError.textContent = error.message || 'No se pudo eliminar el documento.';
                uploadError.classList.remove('hidden');
            }
        }
    }

    function open() {
        modal.classList.remove('hidden');
        modal.classList.add('flex');
        document.body.classList.add('overflow-hidden');
        loadCatalog();
    }

    function close() {
        modal.classList.add('hidden');
        modal.classList.remove('flex');
        document.body.classList.remove('overflow-hidden');
    }

    document.getElementById('documentManagerClose')?.addEventListener('click', close);
    document.getElementById('documentManagerCancel')?.addEventListener('click', close);
    document.getElementById('documentManagerConfirm')?.addEventListener('click', function () {
        syncHiddenInputs();
        close();
    });

    uploadButton?.addEventListener('click', function () { fileInput?.click(); });
    fileInput?.addEventListener('change', function () { uploadFile(fileInput.files?.[0]); });
    searchElement?.addEventListener('input', function () {
        searchTerm = searchElement.value || '';
        render();
    });

    modal.addEventListener('click', function (event) {
        if (event.target === modal) close();
    });

    document.addEventListener('keydown', function (event) {
        if (event.key === 'Escape' && !modal.classList.contains('hidden')) close();
    });

    window.openDocumentManagerModal = function (event) {
        if (event) event.preventDefault();
        open();
    };

    window.removeArticleDocument = function (id) {
        selectedIds.delete(id);
        syncHiddenInputs();
        render();
    };
})();
