/**
 * Editor por Bloques estilo Gutenberg para Panel Administrativo
 * Soporta:
 * - Bubble Menu flotante al seleccionar texto (negrita, cursiva, enlaces, títulos)
 * - Menú desplegable Slash Command ('/') para insertar bloques rápidamente
 * - Drag Handle (⋮⋮) para reordenar bloques
 * - Atajos Markdown en tiempo real (#, ##, >, -)
 * - Inserción y subida de imágenes optimizadas
 */

class BlockEditor {
    constructor(containerId, inputId) {
        this.container = document.getElementById(containerId);
        this.hiddenInput = document.getElementById(inputId);
        if (!this.container || !this.hiddenInput) return;

        this.blocks = [];
        this.bubbleMenu = null;
        this.slashMenu = null;
        this.activeBlockIndex = null;
        this.draggedIndex = null;

        this.init();
    }

    init() {
        this.createUI();
        this.createBubbleMenu();
        this.createSlashMenu();
        this.loadInitialContent();
        this.bindEvents();
    }

    createUI() {
        this.container.classList.add('block-editor-canvas', 'space-y-3', 'min-h-[350px]', 'p-4', 'rounded-2xl', 'bg-white', 'border', 'border-slate-200');
    }

    loadInitialContent() {
        const initialHtml = this.hiddenInput.value.trim();
        if (initialHtml) {
            const parser = new DOMParser();
            const doc = parser.parseFromString(initialHtml, 'text/html');
            const nodes = Array.from(doc.body.children);

            if (nodes.length > 0) {
                nodes.forEach(node => {
                    const tag = node.tagName.toLowerCase();
                    if (tag === 'h2') this.addBlock('h2', node.innerHTML);
                    else if (tag === 'h3') this.addBlock('h3', node.innerHTML);
                    else if (tag === 'blockquote') this.addBlock('quote', node.innerHTML);
                    else if (tag === 'ul' || tag === 'ol') this.addBlock('list', node.innerHTML);
                    else if (tag === 'figure' || tag === 'img') {
                        const img = tag === 'img' ? node : node.querySelector('img');
                        const caption = node.querySelector('figcaption')?.innerText || '';
                        if (img) this.addBlock('image', img.getAttribute('src'), { alt: img.getAttribute('alt') || '', caption });
                    } else {
                        this.addBlock('paragraph', node.innerHTML);
                    }
                });
                return;
            }
        }
        // Si no hay contenido, iniciar con un párrafo vacío
        this.addBlock('paragraph', '');
    }

    createBubbleMenu() {
        const menu = document.createElement('div');
        menu.className = 'fixed z-50 hidden bg-slate-900 text-white rounded-xl shadow-xl px-2 py-1.5 flex items-center gap-1 text-xs border border-white/10';
        menu.innerHTML = `
            <button type="button" data-cmd="bold" class="p-1.5 hover:bg-white/20 rounded font-bold transition">B</button>
            <button type="button" data-cmd="italic" class="p-1.5 hover:bg-white/20 rounded italic transition">I</button>
            <button type="button" data-cmd="createLink" class="p-1.5 hover:bg-white/20 rounded transition">🔗</button>
            <div class="h-4 w-px bg-white/20 mx-1"></div>
            <button type="button" data-block="h2" class="p-1.5 hover:bg-white/20 rounded font-semibold transition">H2</button>
            <button type="button" data-block="h3" class="p-1.5 hover:bg-white/20 rounded font-semibold transition">H3</button>
            <button type="button" data-block="quote" class="p-1.5 hover:bg-white/20 rounded font-semibold transition">“</button>
        `;

        document.body.appendChild(menu);
        this.bubbleMenu = menu;

        menu.addEventListener('mousedown', (e) => {
            e.preventDefault();
            const btn = e.target.closest('button');
            if (!btn) return;

            const cmd = btn.dataset.cmd;
            const blockType = btn.dataset.block;

            if (cmd === 'createLink') {
                const url = prompt('Ingrese el enlace URL:');
                if (url) document.execCommand('createLink', false, url);
            } else if (cmd) {
                document.execCommand(cmd, false, null);
            } else if (blockType && this.activeBlockIndex !== null) {
                this.changeBlockType(this.activeBlockIndex, blockType);
            }
            this.syncContent();
        });
    }

    createSlashMenu() {
        const slash = document.createElement('div');
        slash.className = 'fixed z-50 hidden bg-white text-slate-800 rounded-xl shadow-2xl p-2 w-64 border border-slate-200 text-sm';
        slash.innerHTML = `
            <div class="px-2 py-1 text-[11px] font-bold text-slate-400 uppercase tracking-wider">Insertar Bloque</div>
            <button type="button" data-type="paragraph" class="w-full text-left px-3 py-2 rounded-lg hover:bg-slate-100 flex items-center gap-2.5 transition">
                <span class="w-6 h-6 rounded bg-slate-100 flex items-center justify-center font-serif text-slate-600 font-bold">P</span>
                <div><div class="font-medium text-xs">Párrafo</div><div class="text-[10px] text-slate-400">Texto continuo estándar</div></div>
            </button>
            <button type="button" data-type="h2" class="w-full text-left px-3 py-2 rounded-lg hover:bg-slate-100 flex items-center gap-2.5 transition">
                <span class="w-6 h-6 rounded bg-blue-50 flex items-center justify-center font-bold text-[#003F88] text-xs">H2</span>
                <div><div class="font-medium text-xs">Encabezado Grande</div><div class="text-[10px] text-slate-400">Sección principal</div></div>
            </button>
            <button type="button" data-type="h3" class="w-full text-left px-3 py-2 rounded-lg hover:bg-slate-100 flex items-center gap-2.5 transition">
                <span class="w-6 h-6 rounded bg-blue-50 flex items-center justify-center font-bold text-[#003F88] text-xs">H3</span>
                <div><div class="font-medium text-xs">Subtítulo</div><div class="text-[10px] text-slate-400">Subsección</div></div>
            </button>
            <button type="button" data-type="quote" class="w-full text-left px-3 py-2 rounded-lg hover:bg-slate-100 flex items-center gap-2.5 transition">
                <span class="w-6 h-6 rounded bg-amber-50 flex items-center justify-center font-serif text-amber-700 text-sm">“</span>
                <div><div class="font-medium text-xs">Cita Destacada</div><div class="text-[10px] text-slate-400">Frase relevante o bajada</div></div>
            </button>
            <button type="button" data-type="image" class="w-full text-left px-3 py-2 rounded-lg hover:bg-slate-100 flex items-center gap-2.5 transition">
                <span class="w-6 h-6 rounded bg-emerald-50 flex items-center justify-center text-emerald-600 text-sm">🖼️</span>
                <div><div class="font-medium text-xs">Imagen</div><div class="text-[10px] text-slate-400">Subir variante WebP</div></div>
            </button>
        `;

        document.body.appendChild(slash);
        this.slashMenu = slash;

        slash.addEventListener('click', (e) => {
            const btn = e.target.closest('button');
            if (!btn) return;
            const type = btn.dataset.type;
            if (this.activeBlockIndex !== null) {
                // Limpiar el slash del contenido
                const block = this.blocks[this.activeBlockIndex];
                if (block && block.contentEl) {
                    block.contentEl.innerText = block.contentEl.innerText.replace(/\/$/, '');
                }
                if (type === 'image') {
                    this.triggerImageUpload(this.activeBlockIndex);
                } else {
                    this.changeBlockType(this.activeBlockIndex, type);
                }
            }
            this.hideSlashMenu();
        });
    }

    addBlock(type = 'paragraph', content = '', meta = {}) {
        const index = this.blocks.length;
        const blockEl = document.createElement('div');
        blockEl.className = 'group relative flex items-start gap-2 rounded-xl p-2 transition-colors hover:bg-slate-50/70';
        blockEl.draggable = true;
        blockEl.dataset.index = index;

        // Drag Handle
        const handle = document.createElement('div');
        handle.className = 'cursor-grab select-none opacity-0 group-hover:opacity-100 transition-opacity text-slate-400 hover:text-slate-700 flex items-center justify-center w-6 h-6 rounded hover:bg-slate-200 text-xs font-mono';
        handle.innerHTML = '⋮⋮';
        handle.title = 'Arrastrar o reordenar';

        // Acciones rápidas (Eliminar / Mover)
        const actions = document.createElement('div');
        actions.className = 'opacity-0 group-hover:opacity-100 transition-opacity flex items-center gap-1 text-slate-400';
        actions.innerHTML = `
            <button type="button" class="del-btn p-1 hover:text-rose-600 transition text-xs" title="Eliminar bloque">✕</button>
        `;

        // Contenedor de contenido
        const contentEl = document.createElement('div');
        contentEl.className = 'flex-1 outline-none min-h-[1.5rem]';

        if (type === 'image') {
            contentEl.innerHTML = `
                <div class="rounded-xl overflow-hidden border border-slate-200 bg-slate-100 text-center my-2">
                    <img src="${content}" alt="${meta.alt || ''}" class="max-h-96 mx-auto object-cover rounded-lg w-full" />
                    <input type="text" placeholder="Leyenda o pie de foto..." value="${meta.caption || ''}"
                           class="w-full text-xs text-center p-2 text-slate-500 bg-transparent border-t border-slate-200 outline-none" />
                </div>
            `;
            contentEl.querySelector('input')?.addEventListener('input', () => this.syncContent());
        } else {
            contentEl.contentEditable = true;
            this.applyBlockStyles(contentEl, type);
            contentEl.innerHTML = content || '';
        }

        blockEl.appendChild(handle);
        blockEl.appendChild(contentEl);
        blockEl.appendChild(actions);

        // Bind events del bloque
        actions.querySelector('.del-btn').addEventListener('click', () => {
            this.removeBlock(blockEl);
        });

        this.bindBlockEvents(blockEl, contentEl, type);

        this.container.appendChild(blockEl);
        this.blocks.push({ el: blockEl, contentEl, type, meta });
        this.syncContent();
        return blockEl;
    }

    applyBlockStyles(el, type) {
        el.className = 'flex-1 outline-none min-h-[1.5rem] transition-all ';
        if (type === 'h2') {
            el.className += 'font-serif text-2xl font-bold text-[#00296B] leading-tight';
        } else if (type === 'h3') {
            el.className += 'font-serif text-xl font-semibold text-[#003F88] leading-snug';
        } else if (type === 'quote') {
            el.className += 'border-l-4 border-[#FDC500] pl-4 italic text-slate-600 font-serif text-base bg-amber-50/40 py-2 rounded-r-lg';
        } else if (type === 'list') {
            el.className += 'list-disc pl-5 text-slate-700 text-sm space-y-1';
        } else {
            el.className += 'text-slate-800 text-sm leading-relaxed';
        }
    }

    bindBlockEvents(blockEl, contentEl, type) {
        // Drag and Drop
        blockEl.addEventListener('dragstart', (e) => {
            this.draggedIndex = parseInt(blockEl.dataset.index);
            e.dataTransfer.effectAllowed = 'move';
        });

        blockEl.addEventListener('dragover', (e) => {
            e.preventDefault();
            e.dataTransfer.dropEffect = 'move';
        });

        blockEl.addEventListener('drop', (e) => {
            e.preventDefault();
            const targetIndex = parseInt(blockEl.dataset.index);
            if (this.draggedIndex !== null && this.draggedIndex !== targetIndex) {
                this.reorderBlocks(this.draggedIndex, targetIndex);
            }
        });

        // Eventos de teclado y Markdown
        contentEl.addEventListener('input', (e) => {
            this.activeBlockIndex = parseInt(blockEl.dataset.index);
            this.handleMarkdownShortcuts(contentEl);
            this.syncContent();
        });

        contentEl.addEventListener('keydown', (e) => {
            this.activeBlockIndex = parseInt(blockEl.dataset.index);

            // Enter crea un nuevo párrafo
            if (e.key === 'Enter' && !e.shiftKey && type !== 'list') {
                e.preventDefault();
                const newBlock = this.addBlock('paragraph', '');
                newBlock.querySelector('[contenteditable]')?.focus();
            }

            // Slash trigger
            if (e.key === '/') {
                setTimeout(() => this.showSlashMenu(contentEl), 10);
            }
        });

        contentEl.addEventListener('focus', () => {
            this.activeBlockIndex = parseInt(blockEl.dataset.index);
        });
    }

    handleMarkdownShortcuts(contentEl) {
        const text = contentEl.innerText;
        if (text.startsWith('# ')) {
            contentEl.innerText = text.substring(2);
            this.changeBlockType(this.activeBlockIndex, 'h2');
        } else if (text.startsWith('## ')) {
            contentEl.innerText = text.substring(3);
            this.changeBlockType(this.activeBlockIndex, 'h3');
        } else if (text.startsWith('> ')) {
            contentEl.innerText = text.substring(2);
            this.changeBlockType(this.activeBlockIndex, 'quote');
        } else if (text.startsWith('- ') || text.startsWith('* ')) {
            contentEl.innerText = text.substring(2);
            this.changeBlockType(this.activeBlockIndex, 'list');
        }
    }

    changeBlockType(index, newType) {
        const block = this.blocks[index];
        if (!block) return;
        block.type = newType;
        this.applyBlockStyles(block.contentEl, newType);
        this.syncContent();
    }

    removeBlock(blockEl) {
        const index = parseInt(blockEl.dataset.index);
        blockEl.remove();
        this.blocks.splice(index, 1);
        this.reindex();
        if (this.blocks.length === 0) this.addBlock('paragraph', '');
        this.syncContent();
    }

    reindex() {
        this.blocks.forEach((b, i) => {
            b.el.dataset.index = i;
        });
    }

    reorderBlocks(from, to) {
        const moved = this.blocks.splice(from, 1)[0];
        this.blocks.splice(to, 0, moved);

        // Reinsertar en DOM
        this.blocks.forEach(b => this.container.appendChild(b.el));
        this.reindex();
        this.syncContent();
    }

    triggerImageUpload(index) {
        const input = document.createElement('input');
        input.type = 'file';
        input.accept = 'image/*';
        input.onchange = async () => {
            if (!input.files || input.files.length === 0) return;
            const file = input.files[0];
            const formData = new FormData();
            formData.append('file', file);

            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            try {
                const res = await fetch('/admin/articulos/upload-inline', {
                    method: 'POST',
                    headers: token ? { 'RequestVerificationToken': token } : {},
                    body: formData
                });
                const data = await res.json();
                if (data.url) {
                    this.addBlock('image', data.url, { alt: data.alt || file.name });
                }
            } catch (err) {
                alert('Error al subir la imagen: ' + err);
            }
        };
        input.click();
    }

    bindEvents() {
        // Bubble menu en selección
        document.addEventListener('selectionchange', () => {
            const selection = window.getSelection();
            if (!selection || selection.isCollapsed) {
                this.bubbleMenu.classList.add('hidden');
                return;
            }

            const range = selection.getRangeAt(0);
            if (!this.container.contains(range.commonAncestorContainer)) {
                this.bubbleMenu.classList.add('hidden');
                return;
            }

            const rect = range.getBoundingClientRect();
            this.bubbleMenu.style.top = `${rect.top - 46 + window.scrollY}px`;
            this.bubbleMenu.style.left = `${rect.left + rect.width / 2 - 120}px`;
            this.bubbleMenu.classList.remove('hidden');
        });

        // Ocultar menús con Escape
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') {
                this.bubbleMenu.classList.add('hidden');
                this.hideSlashMenu();
            }
        });
    }

    showSlashMenu(el) {
        const rect = el.getBoundingClientRect();
        this.slashMenu.style.top = `${rect.bottom + 8 + window.scrollY}px`;
        this.slashMenu.style.left = `${rect.left}px`;
        this.slashMenu.classList.remove('hidden');
    }

    hideSlashMenu() {
        if (this.slashMenu) this.slashMenu.classList.add('hidden');
    }

    syncContent() {
        let html = '';
        this.blocks.forEach(b => {
            if (b.type === 'h2') {
                html += `<h2>${b.contentEl.innerHTML}</h2>\n`;
            } else if (b.type === 'h3') {
                html += `<h3>${b.contentEl.innerHTML}</h3>\n`;
            } else if (b.type === 'quote') {
                html += `<blockquote>${b.contentEl.innerHTML}</blockquote>\n`;
            } else if (b.type === 'list') {
                html += `<ul>${b.contentEl.innerHTML}</ul>\n`;
            } else if (b.type === 'image') {
                const img = b.contentEl.querySelector('img');
                const caption = b.contentEl.querySelector('input')?.value || '';
                if (img) {
                    html += `<figure><img src="${img.src}" alt="${img.alt}" />${caption ? `<figcaption>${caption}</figcaption>` : ''}</figure>\n`;
                }
            } else {
                html += `<p>${b.contentEl.innerHTML}</p>\n`;
            }
        });
        this.hiddenInput.value = html;
    }
}

// Inicialización global
document.addEventListener('DOMContentLoaded', () => {
    if (document.getElementById('editorCanvas')) {
        window.editorInstance = new BlockEditor('editorCanvas', 'ContentInput');
    }
});
