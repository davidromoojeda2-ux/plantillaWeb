/**
 * Editor por Bloques Editorial estilo Gutenberg para Panel Administrativo
 * Mejoras clave implementadas:
 * 1. Barra de herramientas flotante, minimalista y compacta que se activa/desactiva
 *    automáticamente al posarse (hover o touch) sobre cada bloque.
 * 2. Selector directo de tipo de bloque en la barra para cambiar de H2 a H3, Párrafo,
 *    Cita, Lista, etc., conservando íntegramente todo el contenido del bloque.
 * 3. Botón '+' interbloques directo: al hacer clic crea inmediatamente un nuevo bloque
 *    (eliminando el modal popup redundante para un flujo ágil e intuitivo).
 * 4. Iconografía 100% SVG vectorial pura (sin emojis ni caracteres unicode).
 * 5. Estados activos/inactivos fluidos al pasar con el cursor o el dedo.
 */

const BLOCK_ICONS = {
    paragraph: `<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h10"/></svg>`,
    h2: `<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M3 5v14M11 5v14M3 12h8m6 7h4c0-2.5 3-3.5 3-5.5a2.5 2.5 0 00-5 0"/></svg>`,
    h3: `<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M3 5v14M11 5v14M3 12h8m6-2a2 2 0 014 0c0 1.5-2 2-2 3.5m0 0c0 1.5 2 2 2 3.5a2 2 0 01-4 0"/></svg>`,
    ol: `<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h1M4 10h1.5M4 14h1.5M4 18h1.5M9 6h11M9 12h11M9 18h11"/></svg>`,
    ul: `<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h.01M4 12h.01M4 18h.01M9 6h11M9 12h11M9 18h11"/></svg>`,
    table: `<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 10h18M3 14h18m-9-4v8m-7 4h14a2 2 0 002-2V6a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"/></svg>`,
    image: `<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z"/></svg>`,
    quote: `<svg class="w-4 h-4" fill="currentColor" viewBox="0 0 24 24"><path d="M14.017 21v-7.391c0-5.704 3.731-9.57 8.983-10.609l.995 2.151c-2.432.917-3.995 3.638-3.995 5.849h4v10h-9.983zm-14.017 0v-7.391c0-5.704 3.748-9.57 9-10.609l.996 2.151c-2.433.917-3.996 3.638-3.996 5.849h3.983v10h-9.983z"/></svg>`,
    callout: `<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/></svg>`,
    code: `<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 20l4-16m4 4l4 4-4 4M6 16l-4-4 4-4"/></svg>`,
    divider: `<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 12h14"/></svg>`,
    bold: `<svg class="w-3.5 h-3.5" viewBox="0 0 24 24" fill="currentColor"><path d="M15.6 10.79c.97-.67 1.65-1.77 1.65-2.79 0-2.26-1.75-4-4-4H7v14h7.04c2.09 0 3.71-1.7 3.71-3.79 0-1.52-.86-2.82-2.15-3.42zM10 6.5h3c.83 0 1.5.67 1.5 1.5s-.67 1.5-1.5 1.5h-3v-3zm3.5 9H10v-3h3.5c.83 0 1.5.67 1.5 1.5s-.67 1.5-1.5 1.5z"/></svg>`,
    italic: `<svg class="w-3.5 h-3.5" viewBox="0 0 24 24" fill="currentColor"><path d="M10 4v3h2.21l-3.42 8H6v3h8v-3h-2.21l3.42-8H18V4z"/></svg>`,
    link: `<svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13.828 10.172a4 4 0 00-5.656 0l-4 4a4 4 0 105.656 5.656l1.102-1.101m-.758-4.899a4 4 0 005.656 0l4-4a4 4 0 00-5.656-5.656l-1.1 1.1"/></svg>`,
    up: `<svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 15l7-7 7 7"/></svg>`,
    down: `<svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M19 9l-7 7-7-7"/></svg>`,
    trash: `<svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"/></svg>`,
    drag: `<svg class="w-3.5 h-3.5" viewBox="0 0 24 24" fill="currentColor"><circle cx="8.5" cy="6.5" r="1.5"/><circle cx="8.5" cy="12" r="1.5"/><circle cx="8.5" cy="17.5" r="1.5"/><circle cx="15.5" cy="6.5" r="1.5"/><circle cx="15.5" cy="12" r="1.5"/><circle cx="15.5" cy="17.5" r="1.5"/></svg>`,
    plus: `<svg class="w-3.5 h-3.5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><path stroke-linecap="round" stroke-linejoin="round" d="M12 4.5v15m7.5-7.5h-15"/></svg>`,
    chevronDown: `<svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M19 9l-7 7-7-7"/></svg>`
};

const BLOCK_LABELS = {
    paragraph: 'Párrafo',
    h2: 'Encabezado H2',
    h3: 'Subtítulo H3',
    ol: 'Lista Numerada',
    ul: 'Lista Viñetas',
    table: 'Tabla Editorial',
    image: 'Imagen',
    quote: 'Cita Destacada',
    callout: 'Nota / Callout',
    code: 'Código',
    divider: 'Separador'
};

class BlockEditor {
    constructor(containerId, inputId) {
        this.container = document.getElementById(containerId);
        this.hiddenInput = document.getElementById(inputId);
        if (!this.container || !this.hiddenInput) return;

        this.blocks = [];
        this.activeBlockIndex = null;
        this.draggedIndex = null;

        this.floatingToolbar = null;
        this.typeDropdownMenu = null;
        this.slashMenu = null;

        // Toolbar visibility: tracked via a single reference to the block being hovered
        this.toolbarTimeout = null;
        this._hoveredBlockEl = null;  // block element the pointer is currently over
        this._toolbarHovered = false; // pointer is over the toolbar itself

        this.init();
    }

    init() {
        this.createCanvas();
        this.createFloatingToolbar();
        this.createSlashMenu();
        this.loadInitialContent();
        this.refreshCanvasLayout();
        this.bindGlobalEvents();
    }

    createCanvas() {
        this.canvas = document.createElement('div');
        this.canvas.className = 'block-editor-canvas min-h-[380px] p-4 rounded-2xl bg-white border border-slate-200 shadow-inner space-y-1 relative';
        this.container.appendChild(this.canvas);
    }

    /* ─── Barra de Herramientas Flotante Minimalista por Bloque ─────────── */
    createFloatingToolbar() {
        const tb = document.createElement('div');
        tb.className = 'fixed z-50 hidden bg-white/95 backdrop-blur-md text-slate-800 rounded-xl shadow-xl px-2 py-1 flex items-center gap-1.5 text-xs border border-slate-200 transition-all duration-150 select-none';
        tb.innerHTML = `
            <!-- Selector de Tipo de Bloque -->
            <div class="relative">
                <button type="button" id="blockTypeTriggerBtn" class="flex items-center gap-1.5 px-2 py-1 rounded-lg hover:bg-slate-100 font-semibold text-slate-800 transition">
                    <span id="blockTypeIcon" class="text-[#00509D]">${BLOCK_ICONS.paragraph}</span>
                    <span id="blockTypeLabel" class="text-xs">Párrafo</span>
                    <span class="text-slate-400">${BLOCK_ICONS.chevronDown}</span>
                </button>
                <div id="blockTypeDropdown" class="absolute left-0 top-full mt-1.5 w-52 bg-white rounded-xl shadow-2xl border border-slate-200 py-1 hidden z-50 max-h-72 overflow-y-auto">
                    ${Object.entries(BLOCK_LABELS).map(([key, label]) => `
                        <button type="button" data-switch-type="${key}" class="w-full text-left px-3 py-1.5 hover:bg-slate-50 flex items-center gap-2.5 transition text-xs font-medium text-slate-700 hover:text-[#00296B]">
                            <span class="w-5 h-5 flex items-center justify-center text-[#00509D]">${BLOCK_ICONS[key]}</span>
                            <span>${label}</span>
                        </button>
                    `).join('')}
                </div>
            </div>

            <div class="h-4 w-px bg-slate-200 mx-0.5"></div>

            <!-- Formato inline -->
            <button type="button" data-cmd="bold" class="p-1.5 hover:bg-slate-100 hover:text-[#00296B] rounded-lg transition" title="Negrita">${BLOCK_ICONS.bold}</button>
            <button type="button" data-cmd="italic" class="p-1.5 hover:bg-slate-100 hover:text-[#00296B] rounded-lg transition" title="Cursiva">${BLOCK_ICONS.italic}</button>
            <button type="button" data-cmd="createLink" class="p-1.5 hover:bg-slate-100 hover:text-[#00296B] rounded-lg transition" title="Insertar enlace">${BLOCK_ICONS.link}</button>

            <div class="h-4 w-px bg-slate-200 mx-0.5"></div>

            <!-- Acciones de Reordenación y Borrado -->
            <button type="button" id="tbMoveUpBtn" class="p-1.5 hover:bg-slate-100 hover:text-slate-900 rounded-lg transition text-slate-500" title="Subir bloque">${BLOCK_ICONS.up}</button>
            <button type="button" id="tbMoveDownBtn" class="p-1.5 hover:bg-slate-100 hover:text-slate-900 rounded-lg transition text-slate-500" title="Bajar bloque">${BLOCK_ICONS.down}</button>
            <button type="button" id="tbDeleteBtn" class="p-1.5 hover:bg-rose-50 hover:text-rose-600 rounded-lg transition text-slate-400" title="Eliminar bloque">${BLOCK_ICONS.trash}</button>
        `;

        document.body.appendChild(tb);
        this.floatingToolbar = tb;
        this.typeDropdownMenu = tb.querySelector('#blockTypeDropdown');

        // Toggle dropdown de tipos
        const triggerBtn = tb.querySelector('#blockTypeTriggerBtn');
        triggerBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            this.typeDropdownMenu.classList.toggle('hidden');
        });

        // Selección de nuevo tipo de bloque (conservando contenido)
        this.typeDropdownMenu.addEventListener('click', (e) => {
            const btn = e.target.closest('button[data-switch-type]');
            if (!btn) return;
            const newType = btn.dataset.switchType;
            if (this.activeBlockIndex !== null) {
                this.changeBlockType(this.activeBlockIndex, newType);
            }
            this.typeDropdownMenu.classList.add('hidden');
        });

        // Formatos inline (B, I, Link)
        tb.querySelectorAll('button[data-cmd]').forEach(btn => {
            btn.addEventListener('mousedown', (e) => {
                e.preventDefault();
                const cmd = btn.dataset.cmd;
                if (cmd === 'createLink') {
                    const url = prompt('Ingrese el enlace URL:');
                    if (url) document.execCommand('createLink', false, url);
                } else if (cmd) {
                    document.execCommand(cmd, false, null);
                }
                this.syncContent();
            });
        });

        // Mover arriba
        tb.querySelector('#tbMoveUpBtn').addEventListener('click', (e) => {
            e.stopPropagation();
            if (this.activeBlockIndex !== null && this.activeBlockIndex > 0) {
                this.reorderBlocks(this.activeBlockIndex, this.activeBlockIndex - 1);
            }
        });

        // Mover abajo
        tb.querySelector('#tbMoveDownBtn').addEventListener('click', (e) => {
            e.stopPropagation();
            if (this.activeBlockIndex !== null && this.activeBlockIndex < this.blocks.length - 1) {
                this.reorderBlocks(this.activeBlockIndex, this.activeBlockIndex + 1);
            }
        });

        // Eliminar
        tb.querySelector('#tbDeleteBtn').addEventListener('click', (e) => {
            e.stopPropagation();
            if (this.activeBlockIndex !== null && this.blocks[this.activeBlockIndex]) {
                this.removeBlock(this.blocks[this.activeBlockIndex].el);
                this.hideToolbar(0);
            }
        });

        // Pointer over toolbar keeps it visible; leaving triggers hide
        tb.addEventListener('pointerenter', () => {
            this._toolbarHovered = true;
            clearTimeout(this.toolbarTimeout);
        });

        tb.addEventListener('pointerleave', () => {
            this._toolbarHovered = false;
            this._scheduleHide(200);
        });
    }

    showToolbarForBlock(blockEl) {
        if (!blockEl || !this.floatingToolbar) return;
        clearTimeout(this.toolbarTimeout);

        const rect = blockEl.getBoundingClientRect();
        const tb = this.floatingToolbar;

        // Actualizar icono y texto del tipo del bloque actual
        const idx = parseInt(blockEl.dataset.index);
        const block = this.blocks[idx];
        if (block) {
            tb.querySelector('#blockTypeIcon').innerHTML = BLOCK_ICONS[block.type] || BLOCK_ICONS.paragraph;
            tb.querySelector('#blockTypeLabel').innerText = BLOCK_LABELS[block.type] || 'Párrafo';
        }

        // Posicionar justo encima del bloque activo
        tb.style.display = 'flex';
        tb.classList.remove('hidden');

        const tbHeight = 40;
        let top = rect.top - tbHeight + window.scrollY;
        if (rect.top < 60) {
            top = rect.top + 6 + window.scrollY;
        }

        const left = Math.max(16, rect.left);
        tb.style.top = `${top}px`;
        tb.style.left = `${left}px`;
    }

    /**
     * Schedules toolbar hide after `delay` ms.
     * Cancelled if the pointer re-enters a block or the toolbar before the timer fires.
     */
    _scheduleHide(delay = 200) {
        clearTimeout(this.toolbarTimeout);
        this.toolbarTimeout = setTimeout(() => {
            // Only hide when neither block nor toolbar is hovered and no dropdown is open
            const dropdownOpen = this.typeDropdownMenu && !this.typeDropdownMenu.classList.contains('hidden');
            if (!this._hoveredBlockEl && !this._toolbarHovered && !dropdownOpen && this.floatingToolbar) {
                this.floatingToolbar.classList.add('hidden');
                if (this.typeDropdownMenu) this.typeDropdownMenu.classList.add('hidden');
            }
        }, delay);
    }

    hideToolbar(delay = 200) {
        this._hoveredBlockEl = null;
        this._scheduleHide(delay);
    }

    /* ─── Carga Inicial y Parseo ─────────────────────────────────────────── */
    loadInitialContent() {
        const initialHtml = this.hiddenInput.value.trim();
        if (initialHtml) {
            const parser = new DOMParser();
            const doc = parser.parseFromString(initialHtml, 'text/html');
            const nodes = Array.from(doc.body.children);

            if (nodes.length > 0) {
                nodes.forEach(node => {
                    const tag = node.tagName.toLowerCase();

                    if (tag === 'h2') {
                        this.createBlockObject('h2', node.innerHTML);
                    } else if (tag === 'h3') {
                        this.createBlockObject('h3', node.innerHTML);
                    } else if (tag === 'blockquote') {
                        const citeEl = node.querySelector('cite, footer');
                        const cite = citeEl ? citeEl.innerText.replace(/^—\s*/, '') : '';
                        if (citeEl) citeEl.remove();
                        const quoteText = node.innerHTML.replace(/^<p>/, '').replace(/<\/p>$/, '');
                        this.createBlockObject('quote', quoteText, { cite });
                    } else if (tag === 'ol') {
                        this.createBlockObject('ol', node.innerHTML);
                    } else if (tag === 'ul') {
                        this.createBlockObject('ul', node.innerHTML);
                    } else if (tag === 'table' || node.classList.contains('table-responsive-wrapper')) {
                        const tbl = tag === 'table' ? node : node.querySelector('table');
                        if (tbl) {
                            this.createBlockObject('table', tbl.outerHTML);
                        } else {
                            this.createBlockObject('paragraph', node.innerHTML);
                        }
                    } else if (tag === 'figure' || tag === 'img' || (node.querySelector('img') && (tag === 'p' || tag === 'div'))) {
                        const img = tag === 'img' ? node : node.querySelector('img');
                        const caption = node.querySelector('figcaption')?.innerText || '';
                        if (img) {
                            this.createBlockObject('image', img.getAttribute('src') || '', {
                                alt: img.getAttribute('alt') || '',
                                caption
                            });
                            // Si el nodo contenedor tenía texto adicional fuera de la imagen
                            const clone = node.cloneNode(true);
                            clone.querySelectorAll('img, figcaption').forEach(el => el.remove());
                            const remainingText = clone.innerHTML.trim();
                            if (remainingText && remainingText !== '<br>') {
                                this.createBlockObject('paragraph', remainingText);
                            }
                        }
                    } else if (node.classList.contains('callout') || node.hasAttribute('data-callout')) {
                        const type = node.getAttribute('data-callout') ||
                            (node.classList.contains('callout-warning') ? 'warning' :
                             node.classList.contains('callout-tip') ? 'tip' :
                             node.classList.contains('callout-success') ? 'success' : 'info');
                        const bodyEl = node.querySelector('.callout-body') || node;
                        const icon = bodyEl.querySelector('.callout-icon');
                        if (icon) icon.remove();
                        this.createBlockObject('callout', bodyEl.innerHTML, { type });
                    } else if (tag === 'pre') {
                        const code = node.querySelector('code')?.innerText || node.innerText;
                        this.createBlockObject('code', code);
                    } else if (tag === 'hr') {
                        this.createBlockObject('divider', '');
                    } else {
                        this.createBlockObject('paragraph', node.innerHTML);
                    }
                });
                return;
            }
        }
        // Iniciar con un párrafo vacío si no hay contenido
        this.createBlockObject('paragraph', '');
    }

    /* ─── Construcción de Objetos de Bloque ──────────────────────────────── */
    createBlockObject(type = 'paragraph', content = '', meta = {}) {
        const blockEl = document.createElement('div');
        blockEl.className = 'editor-block-item group relative flex items-start gap-2.5 rounded-xl p-2.5 transition-all border border-transparent hover:border-slate-300 hover:shadow-xs hover:bg-slate-50/50';
        blockEl.draggable = true;

        // Barra de Control Lateral (Drag Handle + Subir/Bajar)
        const handleBox = document.createElement('div');
        handleBox.className = 'block-handle-box opacity-0 group-hover:opacity-100 transition-opacity flex flex-col items-center gap-0.5 text-slate-400 pt-1 select-none pointer-events-none group-hover:pointer-events-auto';
        handleBox.innerHTML = `
            <button type="button" class="move-up-btn p-0.5 hover:text-slate-800 text-xs" title="Subir bloque">${BLOCK_ICONS.up}</button>
            <span class="cursor-grab text-slate-400 hover:text-slate-700 text-xs font-mono select-none" title="Arrastrar para reordenar">${BLOCK_ICONS.drag}</span>
            <button type="button" class="move-down-btn p-0.5 hover:text-slate-800 text-xs" title="Bajar bloque">${BLOCK_ICONS.down}</button>
        `;

        // Acciones del bloque (Eliminar)
        const actionBox = document.createElement('div');
        actionBox.className = 'block-action-box opacity-0 group-hover:opacity-100 transition-opacity flex items-center gap-1 text-slate-400 pt-1 pointer-events-none group-hover:pointer-events-auto';
        actionBox.innerHTML = `
            <button type="button" class="del-btn p-1 hover:text-rose-600 hover:bg-rose-50 rounded-md transition" title="Eliminar este bloque">${BLOCK_ICONS.trash}</button>
        `;

        const contentEl = document.createElement('div');
        contentEl.className = 'flex-1 outline-none min-h-[1.5rem]';

        this.renderBlockContent(contentEl, type, content, meta);

        blockEl.appendChild(handleBox);
        blockEl.appendChild(contentEl);
        blockEl.appendChild(actionBox);

        handleBox.querySelector('.move-up-btn').addEventListener('click', (e) => {
            e.stopPropagation();
            const currIdx = parseInt(blockEl.dataset.index);
            if (currIdx > 0) this.reorderBlocks(currIdx, currIdx - 1);
        });

        handleBox.querySelector('.move-down-btn').addEventListener('click', (e) => {
            e.stopPropagation();
            const currIdx = parseInt(blockEl.dataset.index);
            if (currIdx < this.blocks.length - 1) this.reorderBlocks(currIdx, currIdx + 1);
        });

        actionBox.querySelector('.del-btn').addEventListener('click', (e) => {
            e.stopPropagation();
            this.removeBlock(blockEl);
        });

        this.bindBlockEvents(blockEl, contentEl, type);

        const blockObj = { el: blockEl, contentEl, type, meta };
        this.blocks.push(blockObj);
        return blockObj;
    }

    insertBlockAt(index, type = 'paragraph', content = '', meta = {}) {
        const blockEl = document.createElement('div');
        blockEl.className = 'editor-block-item group relative flex items-start gap-2.5 rounded-xl p-2.5 transition-all border border-transparent hover:border-slate-300 hover:shadow-xs hover:bg-slate-50/50';
        blockEl.draggable = true;

        const handleBox = document.createElement('div');
        handleBox.className = 'block-handle-box opacity-0 group-hover:opacity-100 transition-opacity flex flex-col items-center gap-0.5 text-slate-400 pt-1 select-none pointer-events-none group-hover:pointer-events-auto';
        handleBox.innerHTML = `
            <button type="button" class="move-up-btn p-0.5 hover:text-slate-800 text-xs" title="Subir bloque">${BLOCK_ICONS.up}</button>
            <span class="cursor-grab text-slate-400 hover:text-slate-700 text-xs font-mono select-none" title="Arrastrar para reordenar">${BLOCK_ICONS.drag}</span>
            <button type="button" class="move-down-btn p-0.5 hover:text-slate-800 text-xs" title="Bajar bloque">${BLOCK_ICONS.down}</button>
        `;

        const actionBox = document.createElement('div');
        actionBox.className = 'block-action-box opacity-0 group-hover:opacity-100 transition-opacity flex items-center gap-1 text-slate-400 pt-1 pointer-events-none group-hover:pointer-events-auto';
        actionBox.innerHTML = `
            <button type="button" class="del-btn p-1 hover:text-rose-600 hover:bg-rose-50 rounded-md transition" title="Eliminar este bloque">${BLOCK_ICONS.trash}</button>
        `;

        const contentEl = document.createElement('div');
        contentEl.className = 'flex-1 outline-none min-h-[1.5rem]';

        this.renderBlockContent(contentEl, type, content, meta);

        blockEl.appendChild(handleBox);
        blockEl.appendChild(contentEl);
        blockEl.appendChild(actionBox);

        handleBox.querySelector('.move-up-btn').addEventListener('click', (e) => {
            e.stopPropagation();
            const currIdx = parseInt(blockEl.dataset.index);
            if (currIdx > 0) this.reorderBlocks(currIdx, currIdx - 1);
        });

        handleBox.querySelector('.move-down-btn').addEventListener('click', (e) => {
            e.stopPropagation();
            const currIdx = parseInt(blockEl.dataset.index);
            if (currIdx < this.blocks.length - 1) this.reorderBlocks(currIdx, currIdx + 1);
        });

        actionBox.querySelector('.del-btn').addEventListener('click', (e) => {
            e.stopPropagation();
            this.removeBlock(blockEl);
        });

        this.bindBlockEvents(blockEl, contentEl, type);

        const blockObj = { el: blockEl, contentEl, type, meta };
        this.blocks.splice(index, 0, blockObj);

        this.refreshCanvasLayout();
        this.setActiveBlock(blockEl);

        const editable = contentEl.querySelector('[contenteditable="true"]') || (contentEl.isContentEditable ? contentEl : null);
        if (editable) {
            setTimeout(() => editable.focus(), 15);
        }

        this.syncContent();
        return blockEl;
    }

    /* ─── Renderizado del Lienzo y Línea Divisoria '+' Directa ─────────── */
    refreshCanvasLayout() {
        this.canvas.innerHTML = '';

        // Separador inicial superior (índice 0)
        this.canvas.appendChild(this.createDividerElement(0));

        this.blocks.forEach((b, i) => {
            b.el.dataset.index = i;
            this.canvas.appendChild(b.el);

            // Separador posterior para insertar en posición (i + 1)
            this.canvas.appendChild(this.createDividerElement(i + 1));
        });

        this.syncContent();
    }

    createDividerElement(insertIndex) {
        const divider = document.createElement('div');
        divider.className = 'block-insert-divider relative py-2 -my-1 cursor-pointer select-none group/divider';
        divider.dataset.insertIndex = insertIndex;

        divider.innerHTML = `
            <div class="divider-inner flex items-center gap-2 opacity-0 group-hover/divider:opacity-100 transition-opacity duration-150">
                <div class="h-px bg-slate-300 flex-1"></div>
                <button type="button" class="divider-plus-btn w-6 h-6 rounded-full bg-[#00296B] hover:bg-[#00509D] text-white flex items-center justify-center shadow-md hover:scale-110 active:scale-95 transition-transform" title="Insertar nuevo bloque aquí">
                    ${BLOCK_ICONS.plus}
                </button>
                <div class="h-px bg-slate-300 flex-1"></div>
            </div>
        `;

        // Activación al posar cursor o dedo
        divider.addEventListener('mouseenter', () => divider.classList.add('is-active'));
        divider.addEventListener('mouseleave', () => divider.classList.remove('is-active'));
        divider.addEventListener('touchstart', () => divider.classList.add('is-active'), { passive: true });

        // Clic directo en '+': Inserta inmediatamente un nuevo bloque editable (sin modal)
        divider.addEventListener('click', (e) => {
            e.stopPropagation();
            this.insertBlockAt(insertIndex, 'paragraph');
        });

        return divider;
    }

    /* ─── Renderizado de Contenidos por Tipo ─────────────────────────────── */
    renderBlockContent(contentEl, type, content, meta = {}) {
        contentEl.innerHTML = '';

        if (type === 'table') {
            contentEl.contentEditable = false;
            contentEl.innerHTML = this.buildTableEditorHtml(content);
            this.bindTableEvents(contentEl);
        } else if (type === 'image') {
            contentEl.contentEditable = false;
            contentEl.innerHTML = this.buildImageEditorHtml(content, meta);
            this.bindImageEvents(contentEl, meta);
        } else if (type === 'quote') {
            contentEl.contentEditable = false;
            contentEl.innerHTML = `
                <div class="border-l-4 border-[#FDC500] bg-amber-50/50 p-3.5 rounded-r-xl space-y-2">
                    <div class="quote-text font-serif italic text-slate-700 text-base outline-none min-h-[1.5rem]" contenteditable="true" placeholder="Escribe la cita memorable...">${content || ''}</div>
                    <div class="flex items-center gap-2 pt-1 border-t border-amber-200/60 text-xs">
                        <span class="text-amber-700 font-bold select-none">—</span>
                        <input type="text" class="quote-cite flex-1 bg-transparent border-none outline-none text-slate-600 font-medium placeholder:text-amber-800/40"
                               placeholder="Autor o fuente de la cita (ej. Redacción Especial, 2026)" value="${meta.cite || ''}" />
                    </div>
                </div>
            `;
            const textEl = contentEl.querySelector('.quote-text');
            const citeEl = contentEl.querySelector('.quote-cite');
            textEl.addEventListener('input', () => this.syncContent());
            citeEl.addEventListener('input', () => {
                const b = this.getBlockFromElement(contentEl);
                if (b) b.meta.cite = citeEl.value;
                this.syncContent();
            });
        } else if (type === 'callout') {
            contentEl.contentEditable = false;
            const calloutType = meta.type || 'info';
            contentEl.innerHTML = this.buildCalloutHtml(calloutType, content);
            this.bindCalloutEvents(contentEl, meta);
        } else if (type === 'code') {
            contentEl.contentEditable = false;
            contentEl.innerHTML = `
                <div class="rounded-xl overflow-hidden bg-slate-900 border border-slate-800 text-slate-100 p-3.5 font-mono text-xs">
                    <div class="flex items-center justify-between text-[11px] text-slate-400 pb-2 border-b border-slate-800 select-none">
                        <span>Fragmento de código / Documento técnico</span>
                        <span class="text-[10px] text-slate-500">Formato predeterminado</span>
                    </div>
                    <pre class="outline-none pt-2 font-mono whitespace-pre-wrap" contenteditable="true" placeholder="// Pega o escribe el código aquí...">${content || ''}</pre>
                </div>
            `;
            contentEl.querySelector('pre').addEventListener('input', () => this.syncContent());
        } else if (type === 'divider') {
            contentEl.contentEditable = false;
            contentEl.innerHTML = `
                <div class="py-3 flex items-center justify-center select-none">
                    <div class="h-px bg-slate-200 flex-1"></div>
                    <span class="px-3 text-slate-400 text-xs font-mono">◆ ◆ ◆</span>
                    <div class="h-px bg-slate-200 flex-1"></div>
                </div>
            `;
        } else if (type === 'ol') {
            contentEl.contentEditable = true;
            this.applyBlockStyles(contentEl, 'ol');
            contentEl.innerHTML = content || '<li>Primer punto numerado...</li><li>Segundo punto numerado...</li>';
        } else if (type === 'ul') {
            contentEl.contentEditable = true;
            this.applyBlockStyles(contentEl, 'ul');
            contentEl.innerHTML = content || '<li>Elemento de la lista...</li><li>Otro elemento destacado...</li>';
        } else {
            contentEl.contentEditable = true;
            this.applyBlockStyles(contentEl, type);
            contentEl.innerHTML = content || '';
        }
    }

    applyBlockStyles(el, type) {
        el.className = 'flex-1 outline-none min-h-[1.5rem] transition-all ';
        if (type === 'h2') {
            el.className += 'font-serif text-2xl font-black text-[#00296B] leading-tight';
        } else if (type === 'h3') {
            el.className += 'font-serif text-xl font-bold text-[#003F88] leading-snug';
        } else if (type === 'ol') {
            el.className += 'article-ordered-list list-decimal pl-6 space-y-1.5 text-slate-800 text-sm leading-relaxed';
        } else if (type === 'ul') {
            el.className += 'article-bullet-list list-disc pl-6 space-y-1.5 text-slate-800 text-sm leading-relaxed';
        } else {
            el.className += 'text-slate-800 text-sm leading-relaxed';
        }
    }

    /* ─── Cambio de Tipo de Bloque Sin Perder Contenido ─────────────────── */
    changeBlockType(index, newType) {
        const block = this.blocks[index];
        if (!block) return;

        // Extraer el texto o HTML actual del bloque para no perder nada
        let currentText = '';
        if (block.type === 'quote') {
            currentText = block.contentEl.querySelector('.quote-text')?.innerHTML.trim() || '';
        } else if (block.type === 'callout') {
            currentText = block.contentEl.querySelector('.callout-editable-content')?.innerHTML.trim() || '';
        } else if (block.type === 'code') {
            currentText = block.contentEl.querySelector('pre')?.innerText.trim() || '';
        } else if (block.type === 'ol' || block.type === 'ul') {
            const lis = Array.from(block.contentEl.querySelectorAll('li')).map(li => li.innerHTML.trim()).filter(Boolean);
            if (lis.length > 0) {
                currentText = newType === 'ol' || newType === 'ul'
                    ? lis.map(item => `<li>${item}</li>`).join('')
                    : lis.join('<br>');
            } else {
                currentText = block.contentEl.innerHTML.trim();
            }
        } else if (block.type === 'image') {
            currentText = block.contentEl.querySelector('.img-caption-input')?.value.trim() || '';
        } else if (block.type === 'table') {
            currentText = '';
        } else {
            currentText = block.contentEl.innerHTML.trim();
        }

        // Si se cambia a lista y viene texto corrido, encapsularlo en <li>
        if ((newType === 'ol' || newType === 'ul') && currentText && !currentText.includes('<li>')) {
            currentText = `<li>${currentText}</li>`;
        }

        block.type = newType;
        this.renderBlockContent(block.contentEl, newType, currentText, block.meta || {});
        this.setActiveBlock(block.el);
        this.syncContent();

        // Actualizar etiqueta del botón en la barra flotante
        if (this.floatingToolbar) {
            this.floatingToolbar.querySelector('#blockTypeIcon').innerHTML = BLOCK_ICONS[newType] || BLOCK_ICONS.paragraph;
            this.floatingToolbar.querySelector('#blockTypeLabel').innerText = BLOCK_LABELS[newType] || 'Párrafo';
        }
    }

    /* ─── Tabla Editorial Editor ─────────────────────────────────────────── */
    buildTableEditorHtml(initialTableHtml) {
        let tableContent = '';
        if (initialTableHtml && initialTableHtml.includes('<table')) {
            const parser = new DOMParser();
            const doc = parser.parseFromString(initialTableHtml, 'text/html');
            const tbl = doc.querySelector('table');
            if (tbl) tableContent = tbl.innerHTML;
        }

        if (!tableContent) {
            tableContent = `
                <thead>
                    <tr>
                        <th class="p-2 border border-slate-300 bg-[#00296B] text-white font-bold text-xs">Columna 1</th>
                        <th class="p-2 border border-slate-300 bg-[#00296B] text-white font-bold text-xs">Columna 2</th>
                        <th class="p-2 border border-slate-300 bg-[#00296B] text-white font-bold text-xs">Columna 3</th>
                    </tr>
                </thead>
                <tbody>
                    <tr>
                        <td class="p-2 border border-slate-200 text-xs">Dato 1</td>
                        <td class="p-2 border border-slate-200 text-xs">Dato 2</td>
                        <td class="p-2 border border-slate-200 text-xs">Dato 3</td>
                    </tr>
                    <tr>
                        <td class="p-2 border border-slate-200 text-xs">Dato 4</td>
                        <td class="p-2 border border-slate-200 text-xs">Dato 5</td>
                        <td class="p-2 border border-slate-200 text-xs">Dato 6</td>
                    </tr>
                </tbody>
            `;
        }

        return `
            <div class="table-editor-widget border border-slate-200 rounded-xl overflow-hidden bg-white shadow-xs my-1">
                <div class="bg-slate-100/90 border-b border-slate-200 px-3 py-1.5 flex items-center justify-between text-xs select-none">
                    <span class="font-bold text-[#00296B] flex items-center gap-1.5">
                        <span class="text-[#00509D]">${BLOCK_ICONS.table}</span>
                        Tabla Editorial
                    </span>
                    <div class="flex items-center gap-1">
                        <button type="button" class="tbl-add-row px-2 py-1 bg-white border border-slate-300 hover:bg-slate-50 text-slate-700 rounded-md font-semibold text-[11px] transition shadow-2xs">
                            + Fila
                        </button>
                        <button type="button" class="tbl-del-row px-2 py-1 bg-white border border-slate-300 hover:bg-rose-50 hover:text-rose-700 text-slate-700 rounded-md font-semibold text-[11px] transition shadow-2xs">
                            - Fila
                        </button>
                        <button type="button" class="tbl-add-col px-2 py-1 bg-white border border-slate-300 hover:bg-slate-50 text-slate-700 rounded-md font-semibold text-[11px] transition shadow-2xs">
                            + Columna
                        </button>
                        <button type="button" class="tbl-del-col px-2 py-1 bg-white border border-slate-300 hover:bg-rose-50 hover:text-rose-700 text-slate-700 rounded-md font-semibold text-[11px] transition shadow-2xs">
                            - Columna
                        </button>
                    </div>
                </div>
                <div class="overflow-x-auto p-3">
                    <table class="article-table w-full border-collapse">
                        ${tableContent}
                    </table>
                </div>
            </div>
        `;
    }

    bindTableEvents(contentEl) {
        const table = contentEl.querySelector('table');
        if (!table) return;

        table.querySelectorAll('th, td').forEach(cell => {
            cell.contentEditable = true;
            cell.addEventListener('input', () => this.syncContent());
        });

        contentEl.querySelector('.tbl-add-row')?.addEventListener('click', () => {
            const tbody = table.querySelector('tbody') || table;
            const colsCount = table.querySelector('tr')?.children.length || 3;
            const tr = document.createElement('tr');
            for (let i = 0; i < colsCount; i++) {
                const td = document.createElement('td');
                td.className = 'p-2 border border-slate-200 text-xs';
                td.contentEditable = true;
                td.innerText = 'Nuevo dato';
                td.addEventListener('input', () => this.syncContent());
                tr.appendChild(td);
            }
            tbody.appendChild(tr);
            this.syncContent();
        });

        contentEl.querySelector('.tbl-del-row')?.addEventListener('click', () => {
            const tbody = table.querySelector('tbody');
            if (tbody && tbody.rows.length > 1) {
                tbody.deleteRow(-1);
                this.syncContent();
            }
        });

        contentEl.querySelector('.tbl-add-col')?.addEventListener('click', () => {
            const theadTr = table.querySelector('thead tr');
            if (theadTr) {
                const th = document.createElement('th');
                th.className = 'p-2 border border-slate-300 bg-[#00296B] text-white font-bold text-xs';
                th.contentEditable = true;
                th.innerText = 'Nueva Columna';
                th.addEventListener('input', () => this.syncContent());
                theadTr.appendChild(th);
            }
            const tbodyRows = table.querySelectorAll('tbody tr');
            tbodyRows.forEach(tr => {
                const td = document.createElement('td');
                td.className = 'p-2 border border-slate-200 text-xs';
                td.contentEditable = true;
                td.innerText = 'Dato';
                td.addEventListener('input', () => this.syncContent());
                tr.appendChild(td);
            });
            this.syncContent();
        });

        contentEl.querySelector('.tbl-del-col')?.addEventListener('click', () => {
            const allRows = table.querySelectorAll('tr');
            if (allRows.length > 0 && allRows[0].children.length > 1) {
                allRows.forEach(tr => tr.lastElementChild?.remove());
                this.syncContent();
            }
        });
    }

    /* ─── Bloque de Imagen Enriquecido ───────────────────────────────────── */
    buildImageEditorHtml(src, meta = {}) {
        const hasSrc = !!src;
        return `
            <div class="image-block-widget border border-slate-200 rounded-2xl overflow-hidden bg-slate-50/70 p-5 space-y-4 shadow-2xs">
                <!-- Previsualización de la Imagen -->
                <div class="image-preview-box text-center bg-white border border-slate-200 rounded-xl overflow-hidden relative ${hasSrc ? '' : 'hidden'} shadow-xs">
                    <img src="${src || ''}" alt="${meta.alt || ''}" referrerpolicy="no-referrer"
                         class="max-h-80 mx-auto object-cover rounded-lg w-full transition-opacity"
                         onload="this.parentElement.querySelector('.img-error-badge')?.classList.add('hidden'); this.classList.remove('opacity-30');"
                         onerror="this.parentElement.querySelector('.img-error-badge')?.classList.remove('hidden'); this.classList.add('opacity-30');" />
                    <div class="img-error-badge hidden absolute inset-0 flex items-center justify-center bg-slate-950/85 text-amber-200 text-xs font-semibold px-4 text-center">
                        ⚠️ No se pudo previsualizar la imagen en línea (comprueba la URL o conexión)
                    </div>
                </div>

                <!-- Fila 1: Ruta o URL con acciones alineadas y espacio completo -->
                <div class="space-y-1.5">
                    <div class="flex flex-wrap items-center justify-between gap-2">
                        <label class="block font-bold text-slate-700 text-xs tracking-wide">
                            Ruta o URL de la Imagen
                        </label>
                        <div class="flex items-center gap-2">
                            <button type="button" class="img-library-btn px-3 py-1.5 bg-blue-50 hover:bg-blue-100 text-[#00296B] font-bold rounded-lg border border-blue-200 transition-all flex items-center gap-1.5 text-xs shadow-2xs hover:shadow-xs active:scale-95 cursor-pointer" title="Explorar Biblioteca de Medios">
                                ${BLOCK_ICONS.image}
                                <span>Biblioteca</span>
                            </button>
                            <button type="button" class="img-upload-btn px-3 py-1.5 bg-[#00296B] hover:bg-[#003F88] text-white font-bold rounded-lg shadow-xs transition-all flex items-center gap-1.5 text-xs active:scale-95 cursor-pointer" title="Subir archivo desde el equipo">
                                <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 8m4-4v12"/></svg>
                                <span>Subir</span>
                            </button>
                            <input type="file" accept="image/*" class="img-file-hidden hidden" />
                        </div>
                    </div>
                    <input type="text" class="img-url-input w-full px-3.5 py-2.5 bg-white rounded-xl border border-slate-300 text-slate-800 font-mono text-xs outline-none focus:ring-2 focus:ring-[#00509D] focus:border-transparent transition shadow-2xs"
                           placeholder="https://ejemplo.com/imagen.jpg o /uploads/..." value="${src || ''}" />
                </div>

                <!-- Fila 2: Cuadrícula equilibrada para SEO Alt y Pie de Foto -->
                <div class="grid grid-cols-1 md:grid-cols-2 gap-4 text-xs pt-0.5">
                    <div class="space-y-1.5">
                        <label class="block font-bold text-slate-700 text-xs tracking-wide">
                            Texto Alternativo (SEO / Alt)
                        </label>
                        <input type="text" class="img-alt-input w-full px-3.5 py-2.5 bg-white rounded-xl border border-slate-300 text-slate-800 text-xs outline-none focus:ring-2 focus:ring-[#00509D] focus:border-transparent transition shadow-2xs"
                               placeholder="Descripción breve de la imagen para accesibilidad..." value="${meta.alt || ''}" />
                    </div>
                    <div class="space-y-1.5">
                        <label class="block font-bold text-slate-700 text-xs tracking-wide">
                            Pie de Foto / Leyenda Editorial (Figcaption)
                        </label>
                        <input type="text" class="img-caption-input w-full px-3.5 py-2.5 bg-white rounded-xl border border-slate-300 text-slate-600 text-xs outline-none focus:ring-2 focus:ring-[#00509D] focus:border-transparent transition shadow-2xs"
                               placeholder="Leyenda que se mostrará debajo de la imagen..." value="${meta.caption || ''}" />
                    </div>
                </div>
            </div>
        `;
    }

    bindImageEvents(contentEl, meta) {
        const urlInput = contentEl.querySelector('.img-url-input');
        const altInput = contentEl.querySelector('.img-alt-input');
        const captionInput = contentEl.querySelector('.img-caption-input');
        const previewBox = contentEl.querySelector('.image-preview-box');
        const previewImg = contentEl.querySelector('.image-preview-box img');
        const uploadBtn = contentEl.querySelector('.img-upload-btn');
        const libraryBtn = contentEl.querySelector('.img-library-btn');
        const fileInput = contentEl.querySelector('.img-file-hidden');
        const errorBadge = contentEl.querySelector('.img-error-badge');

        urlInput?.addEventListener('input', () => {
            const val = urlInput.value.trim();
            if (val) {
                previewImg.src = val;
                previewImg.referrerPolicy = 'no-referrer';
                previewBox.classList.remove('hidden');
                errorBadge?.classList.add('hidden');
                previewImg.classList.remove('opacity-30');
            } else {
                previewBox.classList.add('hidden');
            }
            this.syncContent();
        });

        libraryBtn?.addEventListener('click', (e) => {
            e.preventDefault();
            e.stopPropagation();
            this.openMediaPicker((selectedImg) => {
                const chosenUrl = selectedImg.url || selectedImg.thumbnailUrl;
                if (!chosenUrl) return;
                urlInput.value = chosenUrl;
                previewImg.src = chosenUrl;
                previewImg.referrerPolicy = 'no-referrer';
                previewBox.classList.remove('hidden');
                errorBadge?.classList.add('hidden');
                previewImg.classList.remove('opacity-30');
                if (!altInput.value && (selectedImg.alt || selectedImg.fileName)) {
                    altInput.value = selectedImg.alt || selectedImg.fileName;
                }
                this.syncContent();
            });
        });

        altInput?.addEventListener('input', () => {
            previewImg.alt = altInput.value;
            this.syncContent();
        });

        captionInput?.addEventListener('input', () => this.syncContent());

        uploadBtn?.addEventListener('click', () => fileInput?.click());

        fileInput?.addEventListener('change', async () => {
            if (!fileInput.files || fileInput.files.length === 0) return;
            const file = fileInput.files[0];
            const formData = new FormData();
            formData.append('file', file);

            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            uploadBtn.disabled = true;
            uploadBtn.innerText = 'Subiendo...';

            try {
                const res = await fetch('/admin/articulos/upload-inline', {
                    method: 'POST',
                    headers: token ? { 'RequestVerificationToken': token } : {},
                    body: formData
                });
                const data = await res.json();
                if (data.url) {
                    urlInput.value = data.url;
                    previewImg.src = data.url;
                    previewImg.referrerPolicy = 'no-referrer';
                    previewBox.classList.remove('hidden');
                    errorBadge?.classList.add('hidden');
                    previewImg.classList.remove('opacity-30');
                    if (!altInput.value) altInput.value = data.alt || file.name.split('.')[0];
                    this.syncContent();
                } else if (data.error) {
                    alert(data.error);
                }
            } catch (err) {
                alert('Error al subir imagen: ' + err);
            } finally {
                uploadBtn.disabled = false;
                uploadBtn.innerHTML = `
                    <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 8m4-4v12"/></svg>
                    <span>Subir</span>
                `;
            }
        });
    }

    /* ─── Selector de Medios (Integrado o con Fallback Autónomo) ───────── */
    openMediaPicker(callback) {
        if (typeof window.openMediaLibraryModalForInline === 'function') {
            window.openMediaLibraryModalForInline(callback);
            return;
        }
        this.showBuiltInMediaLibraryModal(callback);
    }

    async showBuiltInMediaLibraryModal(callback) {
        let modal = document.getElementById('editorBuiltInMediaModal');
        if (!modal) {
            modal = document.createElement('div');
            modal.id = 'editorBuiltInMediaModal';
            modal.className = 'fixed inset-0 z-[100] bg-slate-900/60 backdrop-blur-xs flex items-center justify-center p-4';
            modal.innerHTML = `
                <div class="bg-white rounded-2xl shadow-2xl border border-slate-200 max-w-4xl w-full flex flex-col max-h-[88vh] overflow-hidden animate-in fade-in zoom-in-95 duration-150">
                    <div class="p-5 border-b border-slate-100 flex items-center justify-between bg-slate-50/70">
                        <div class="flex items-center gap-2.5">
                            <span class="p-2 bg-blue-100 text-[#00509D] rounded-xl">${BLOCK_ICONS.image}</span>
                            <div>
                                <h3 class="text-base font-bold text-slate-800">Biblioteca de Medios</h3>
                                <p class="text-xs text-slate-500">Selecciona una imagen disponible para insertarla en el artículo</p>
                            </div>
                        </div>
                        <button type="button" class="modal-close-btn text-slate-400 hover:text-slate-600 p-1.5 rounded-lg hover:bg-slate-200/60 transition cursor-pointer">
                            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/></svg>
                        </button>
                    </div>
                    <div class="p-4 border-b border-slate-100 bg-white">
                        <input type="text" class="modal-search-input w-full px-3.5 py-2 text-xs rounded-xl border border-slate-300 focus:ring-2 focus:ring-[#00509D] outline-none"
                               placeholder="Buscar imágenes por nombre o descripción..." />
                    </div>
                    <div class="modal-grid-container flex-1 overflow-y-auto p-4 bg-slate-50/50 min-h-[300px]">
                        <div class="modal-grid grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 gap-3">
                            <div class="col-span-full py-12 text-center text-slate-400 text-xs">Cargando biblioteca...</div>
                        </div>
                    </div>
                    <div class="p-4 border-t border-slate-100 bg-slate-50 flex items-center justify-between">
                        <button type="button" class="modal-cancel-btn px-4 py-2 rounded-xl border border-slate-300 text-slate-700 text-xs font-semibold hover:bg-slate-100 transition cursor-pointer">
                            Cancelar
                        </button>
                        <button type="button" class="modal-insert-btn px-5 py-2 rounded-xl bg-[#00296B] hover:bg-[#003F88] disabled:opacity-40 disabled:cursor-not-allowed text-white text-xs font-bold transition flex items-center gap-1.5 cursor-pointer" disabled>
                            <span>Insertar en el Artículo</span>
                        </button>
                    </div>
                </div>
            `;
            document.body.appendChild(modal);
        }

        modal.classList.remove('hidden');
        modal.style.display = 'flex';
        document.body.classList.add('overflow-hidden');

        const searchInput = modal.querySelector('.modal-search-input');
        const grid = modal.querySelector('.modal-grid');
        const insertBtn = modal.querySelector('.modal-insert-btn');
        const closeBtn = modal.querySelector('.modal-close-btn');
        const cancelBtn = modal.querySelector('.modal-cancel-btn');

        let selected = null;
        let images = [];

        const closeModal = () => {
            modal.classList.add('hidden');
            modal.style.display = 'none';
            document.body.classList.remove('overflow-hidden');
        };

        closeBtn.onclick = closeModal;
        cancelBtn.onclick = closeModal;
        modal.onclick = (e) => { if (e.target === modal) closeModal(); };

        const render = () => {
            const term = (searchInput.value || '').trim().toLowerCase();
            const filtered = images.filter(img => {
                if (!term) return true;
                return (img.fileName || '').toLowerCase().includes(term) || (img.alt || '').toLowerCase().includes(term);
            });

            grid.innerHTML = '';
            if (filtered.length === 0) {
                grid.innerHTML = `<div class="col-span-full py-12 text-center text-slate-400 text-xs">No se encontraron imágenes en la biblioteca.</div>`;
                insertBtn.disabled = true;
                return;
            }

            filtered.forEach(img => {
                const card = document.createElement('div');
                const isSel = selected && selected.id === img.id;
                card.className = `p-2 rounded-xl border bg-white cursor-pointer transition shadow-2xs hover:shadow-md ${isSel ? 'ring-2 ring-[#00509D] border-[#00509D] bg-blue-50/30' : 'border-slate-200 hover:border-slate-300'}`;
                card.innerHTML = `
                    <div class="aspect-video w-full rounded-lg overflow-hidden bg-slate-100 relative mb-1.5">
                        <img src="${img.thumbnailUrl || img.url}" alt="${img.alt || img.fileName || ''}" class="w-full h-full object-cover" loading="lazy" />
                    </div>
                    <p class="text-[11px] font-semibold text-slate-700 truncate" title="${img.fileName || ''}">${img.fileName || 'Imagen'}</p>
                `;
                card.onclick = () => {
                    selected = img;
                    insertBtn.disabled = false;
                    render();
                };
                card.ondblclick = () => {
                    callback(img);
                    closeModal();
                };
                grid.appendChild(card);
            });
        };

        insertBtn.onclick = () => {
            if (!selected) return;
            callback(selected);
            closeModal();
        };

        searchInput.oninput = () => render();

        try {
            const res = await fetch('/admin/articulos/imagenes-disponibles');
            images = await res.json();
            if (!Array.isArray(images)) images = [];
        } catch (e) {
            console.warn('Error cargando imágenes:', e);
            images = [];
        }
        render();
    }

    /* ─── Bloque Callout Contextual ──────────────────────────────────────── */
    buildCalloutHtml(type = 'info', content = '') {
        const types = [
            { id: 'info', label: 'Info (Azul)', border: 'border-blue-500', bg: 'bg-blue-50/70' },
            { id: 'tip', label: 'Consejo (Ámbar)', border: 'border-amber-500', bg: 'bg-amber-50/70' },
            { id: 'warning', label: 'Alerta (Rojo)', border: 'border-rose-500', bg: 'bg-rose-50/70' },
            { id: 'success', label: 'Éxito (Verde)', border: 'border-emerald-500', bg: 'bg-emerald-50/70' }
        ];

        return `
            <div class="callout-widget border-l-4 rounded-r-xl p-3.5 space-y-2 border-blue-500 bg-blue-50/70" data-current-type="${type}">
                <div class="flex items-center justify-between text-xs select-none pb-1.5 border-b border-black/5">
                    <span class="font-bold flex items-center gap-1.5 callout-title">
                        <span class="callout-icon-badge text-[#00509D]">${BLOCK_ICONS.callout}</span>
                        Cuadro Destacado
                    </span>
                    <div class="flex items-center gap-1 callout-type-buttons">
                        ${types.map(t => `
                            <button type="button" data-callout-type="${t.id}" class="px-2 py-0.5 rounded text-[10px] font-bold transition ${type === t.id ? 'bg-[#00296B] text-white' : 'bg-white/80 text-slate-700 hover:bg-white'}">
                                ${t.label}
                            </button>
                        `).join('')}
                    </div>
                </div>
                <div class="callout-editable-content outline-none text-slate-800 text-sm leading-relaxed min-h-[1.5rem]" contenteditable="true" placeholder="Escribe el mensaje o dato clave a destacar...">${content || ''}</div>
            </div>
        `;
    }

    bindCalloutEvents(contentEl, meta) {
        const widget = contentEl.querySelector('.callout-widget');
        const editable = contentEl.querySelector('.callout-editable-content');
        const typeButtons = contentEl.querySelectorAll('[data-callout-type]');

        editable?.addEventListener('input', () => this.syncContent());

        typeButtons.forEach(btn => {
            btn.addEventListener('click', () => {
                const newType = btn.dataset.calloutType;
                widget.dataset.currentType = newType;
                meta.type = newType;

                typeButtons.forEach(b => {
                    b.className = 'px-2 py-0.5 rounded text-[10px] font-bold transition ' +
                        (b.dataset.calloutType === newType ? 'bg-[#00296B] text-white' : 'bg-white/80 text-slate-700 hover:bg-white');
                });

                if (newType === 'warning') {
                    widget.className = 'callout-widget border-l-4 rounded-r-xl p-3.5 space-y-2 border-rose-500 bg-rose-50/70';
                } else if (newType === 'tip') {
                    widget.className = 'callout-widget border-l-4 rounded-r-xl p-3.5 space-y-2 border-amber-500 bg-amber-50/70';
                } else if (newType === 'success') {
                    widget.className = 'callout-widget border-l-4 rounded-r-xl p-3.5 space-y-2 border-emerald-500 bg-emerald-50/70';
                } else {
                    widget.className = 'callout-widget border-l-4 rounded-r-xl p-3.5 space-y-2 border-blue-500 bg-blue-50/70';
                }

                this.syncContent();
            });
        });
    }

    /* ─── Activación de Bloques y Barra Flotante ────────────────────────── */
    setActiveBlock(blockEl) {
        if (!blockEl) return;
        document.querySelectorAll('.editor-block-item').forEach(b => b.classList.remove('is-block-active'));
        blockEl.classList.add('is-block-active');

        const idx = parseInt(blockEl.dataset.index);
        this.activeBlockIndex = isNaN(idx) ? null : idx;

        this.showToolbarForBlock(blockEl);
    }

    deactivateBlock(blockEl) {
        if (blockEl) {
            blockEl.classList.remove('is-block-active');
        }
        // Only clear tracked block if it matches the one leaving
        if (this._hoveredBlockEl === blockEl) {
            this._hoveredBlockEl = null;
        }
        this._scheduleHide(220);
    }

    bindBlockEvents(blockEl, contentEl, type) {
        blockEl.addEventListener('pointerenter', () => {
            this._hoveredBlockEl = blockEl;
            clearTimeout(this.toolbarTimeout);
            this.setActiveBlock(blockEl);
        });

        blockEl.addEventListener('pointerleave', (e) => {
            // Only deactivate if pointer truly left the block (not entering a child element outside the block)
            if (!blockEl.contains(e.relatedTarget)) {
                this.deactivateBlock(blockEl);
            }
        });

        blockEl.addEventListener('touchstart', () => {
            this._hoveredBlockEl = blockEl;
            this.setActiveBlock(blockEl);
        }, { passive: true });

        contentEl.addEventListener('focusin', () => {
            this._hoveredBlockEl = blockEl;
            clearTimeout(this.toolbarTimeout);
            this.setActiveBlock(blockEl);
        });

        contentEl.addEventListener('focusout', (e) => {
            // Only hide if focus truly left the block and toolbar
            setTimeout(() => {
                const active = document.activeElement;
                const inBlock = blockEl.contains(active);
                const inToolbar = this.floatingToolbar?.contains(active);
                if (!inBlock && !inToolbar && !this._hoveredBlockEl && !this._toolbarHovered) {
                    this.deactivateBlock(blockEl);
                }
            }, 80);
        });

        // Drag & Drop
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

        // Markdown Shortcuts y Sync
        contentEl.addEventListener('input', () => {
            this.activeBlockIndex = parseInt(blockEl.dataset.index);
            if (contentEl.isContentEditable) {
                this.handleMarkdownShortcuts(contentEl);
            }
            this.syncContent();
        });

        contentEl.addEventListener('keydown', (e) => {
            this.activeBlockIndex = parseInt(blockEl.dataset.index);

            // Enter crea un nuevo párrafo debajo en texto simple
            if (e.key === 'Enter' && !e.shiftKey && type !== 'ol' && type !== 'ul' && type !== 'table' && type !== 'code') {
                e.preventDefault();
                this.insertBlockAt(this.activeBlockIndex + 1, 'paragraph');
            }

            // Slash trigger
            if (e.key === '/') {
                setTimeout(() => this.showSlashMenu(contentEl), 10);
            }
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
        } else if (text.startsWith('1. ')) {
            const initialItem = text.substring(3) || 'Elemento inicial';
            this.changeBlockType(this.activeBlockIndex, 'ol', `<li>${initialItem}</li>`);
        } else if (text.startsWith('- ') || text.startsWith('* ')) {
            const initialItem = text.substring(2) || 'Elemento inicial';
            this.changeBlockType(this.activeBlockIndex, 'ul', `<li>${initialItem}</li>`);
        } else if (text.startsWith('> ')) {
            contentEl.innerText = text.substring(2);
            this.changeBlockType(this.activeBlockIndex, 'quote');
        } else if (text.trim() === '---') {
            this.changeBlockType(this.activeBlockIndex, 'divider');
        }
    }

    removeBlock(blockEl) {
        const index = parseInt(blockEl.dataset.index);
        this.blocks.splice(index, 1);
        if (this.blocks.length === 0) {
            this.createBlockObject('paragraph', '');
        }
        this.refreshCanvasLayout();
        this.syncContent();
    }

    reorderBlocks(from, to) {
        const moved = this.blocks.splice(from, 1)[0];
        this.blocks.splice(to, 0, moved);
        this.refreshCanvasLayout();
        this.setActiveBlock(moved.el);
    }

    getBlockFromElement(el) {
        const blockEl = el.closest('[data-index]');
        if (!blockEl) return null;
        const idx = parseInt(blockEl.dataset.index);
        return this.blocks[idx] || null;
    }

    /* ─── Menú Slash Command ('/') ───────────────────────────────────────── */
    createSlashMenu() {
        const slash = document.createElement('div');
        slash.className = 'fixed z-50 hidden bg-white text-slate-800 rounded-2xl shadow-2xl p-2 w-72 border border-slate-200 text-sm overflow-hidden select-none';
        slash.innerHTML = `
            <div class="px-3 py-1.5 text-[10px] font-bold text-slate-400 uppercase tracking-wider border-b border-slate-100 mb-1">Cambiar / Insertar Bloque</div>
            <div class="max-h-80 overflow-y-auto space-y-0.5">
                ${Object.entries(BLOCK_LABELS).map(([key, label]) => `
                    <button type="button" data-type="${key}" class="w-full text-left px-3 py-2 rounded-xl hover:bg-slate-100 flex items-center gap-3 transition">
                        <span class="w-7 h-7 rounded-lg bg-slate-100 flex items-center justify-center text-[#00509D] font-bold text-xs">${BLOCK_ICONS[key]}</span>
                        <div class="font-semibold text-xs text-slate-800">${label}</div>
                    </button>
                `).join('')}
            </div>
        `;

        document.body.appendChild(slash);
        this.slashMenu = slash;

        slash.addEventListener('click', (e) => {
            const btn = e.target.closest('button[data-type]');
            if (!btn) return;
            const type = btn.dataset.type;
            if (this.activeBlockIndex !== null) {
                const block = this.blocks[this.activeBlockIndex];
                if (block && block.contentEl) {
                    block.contentEl.innerText = block.contentEl.innerText.replace(/\/$/, '');
                }
                this.changeBlockType(this.activeBlockIndex, type);
            }
            this.hideSlashMenu();
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

    /* ─── Global Listeners ───────────────────────────────────────────────── */
    bindGlobalEvents() {
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') {
                this.hideSlashMenu();
                if (this.typeDropdownMenu) this.typeDropdownMenu.classList.add('hidden');
                this.hideToolbar(0);
            }
        });

        document.addEventListener('click', (e) => {
            if (this.slashMenu && !this.slashMenu.contains(e.target)) {
                this.hideSlashMenu();
            }
            if (this.typeDropdownMenu && !this.typeDropdownMenu.contains(e.target) && !e.target.closest('#blockTypeTriggerBtn')) {
                this.typeDropdownMenu.classList.add('hidden');
            }
            if (!this.container.contains(e.target) && !this.floatingToolbar?.contains(e.target)) {
                document.querySelectorAll('.editor-block-item').forEach(b => b.classList.remove('is-block-active'));
                this._hoveredBlockEl = null;
                this._toolbarHovered = false;
                this.hideToolbar(0);
            }
        });

        // Hide toolbar when pointer moves over the canvas empty area (between blocks)
        this.canvas?.addEventListener('pointermove', (e) => {
            const overBlock = e.target.closest('.editor-block-item');
            if (!overBlock && !this._toolbarHovered) {
                this._hoveredBlockEl = null;
                this._scheduleHide(300);
            }
        });
    }

    /* ─── Sincronización a HTML ─────────────────────────────────────────── */
    syncContent() {
        let html = '';
        this.blocks.forEach(b => {
            if (b.type === 'h2') {
                const text = b.contentEl.innerHTML.trim();
                if (text) html += `<h2>${text}</h2>\n`;
            } else if (b.type === 'h3') {
                const text = b.contentEl.innerHTML.trim();
                if (text) html += `<h3>${text}</h3>\n`;
            } else if (b.type === 'ol') {
                const items = b.contentEl.innerHTML.trim();
                if (items) html += `<ol class="article-ordered-list">\n${items}\n</ol>\n`;
            } else if (b.type === 'ul') {
                const items = b.contentEl.innerHTML.trim();
                if (items) html += `<ul class="article-bullet-list">\n${items}\n</ul>\n`;
            } else if (b.type === 'table') {
                const tbl = b.contentEl.querySelector('table');
                if (tbl) {
                    const clone = tbl.cloneNode(true);
                    clone.querySelectorAll('[contenteditable]').forEach(c => c.removeAttribute('contenteditable'));
                    clone.className = 'article-table';
                    html += `<div class="table-responsive-wrapper">\n${clone.outerHTML}\n</div>\n`;
                }
            } else if (b.type === 'image') {
                const url = b.contentEl.querySelector('.img-url-input')?.value.trim();
                const alt = b.contentEl.querySelector('.img-alt-input')?.value.trim() || '';
                const caption = b.contentEl.querySelector('.img-caption-input')?.value.trim() || '';
                if (url) {
                    html += `<figure class="article-figure">\n<img src="${url}" alt="${alt}" loading="lazy" />\n${caption ? `<figcaption>${caption}</figcaption>\n` : ''}</figure>\n`;
                }
            } else if (b.type === 'quote') {
                const quoteText = b.contentEl.querySelector('.quote-text')?.innerHTML.trim();
                const citeText = b.contentEl.querySelector('.quote-cite')?.value.trim();
                if (quoteText) {
                    html += `<blockquote>\n<p>${quoteText}</p>\n${citeText ? `<cite>${citeText}</cite>\n` : ''}</blockquote>\n`;
                }
            } else if (b.type === 'callout') {
                const widget = b.contentEl.querySelector('.callout-widget');
                const calloutType = widget?.dataset.currentType || b.meta.type || 'info';
                const content = b.contentEl.querySelector('.callout-editable-content')?.innerHTML.trim();
                if (content) {
                    html += `<div class="callout callout-${calloutType}" data-callout="${calloutType}">\n<div class="callout-body">${content}</div>\n</div>\n`;
                }
            } else if (b.type === 'code') {
                const code = b.contentEl.querySelector('pre')?.innerText.trim();
                if (code) {
                    const escaped = code.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
                    html += `<pre><code>${escaped}</code></pre>\n`;
                }
            } else if (b.type === 'divider') {
                html += `<hr class="article-divider" />\n`;
            } else {
                const text = b.contentEl.innerHTML.trim();
                if (text && text !== '<br>') {
                    html += `<p>${text}</p>\n`;
                }
            }
        });

        this.hiddenInput.value = html.trim();
    }
}

// Inicialización global
document.addEventListener('DOMContentLoaded', () => {
    if (document.getElementById('editorCanvas')) {
        window.editorInstance = new BlockEditor('editorCanvas', 'ContentInput');
    }
});
