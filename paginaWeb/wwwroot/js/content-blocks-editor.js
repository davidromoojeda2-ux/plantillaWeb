/**
 * Editor de bloques tipados para artículos y páginas informativas.
 *
 * El editor mantiene el contrato JSON que consume
 * `Aplication.DTOs.ContentBlocks` y escribe el valor en el textarea oculto
 * `BlocksJsonInput`. La sanitización definitiva y los límites se aplican en el
 * servidor; aquí solo se filtran los fragmentos inline para que la previsualización
 * no introduzca etiquetas activas.
 */
(function () {
    'use strict';

    const BLOCK_TYPES = [
        { type: 'paragraph', label: 'Párrafo' },
        { type: 'heading', label: 'Encabezado' },
        { type: 'ordered-list', label: 'Lista numerada' },
        { type: 'bullet-list', label: 'Lista con viñetas' },
        { type: 'quote', label: 'Cita' },
        { type: 'callout', label: 'Callout' },
        { type: 'image', label: 'Imagen' },
        { type: 'table', label: 'Tabla' },
        { type: 'code', label: 'Código' },
        { type: 'divider', label: 'Separador' }
    ];

    const TYPE_LABEL = Object.fromEntries(BLOCK_TYPES.map(item => [item.type, item.label]));
    const ALLOWED_INLINE_TAGS = new Set(['strong', 'b', 'em', 'i', 'u', 's', 'a', 'code', 'br']);
    const DANGEROUS_TAGS = new Set(['script', 'style', 'iframe', 'object', 'embed', 'svg', 'math', 'template', 'noscript']);
    const LINK_SCHEMES = new Set(['http:', 'https:', 'mailto:']);
    const CALLOUT_VARIANTS = ['info', 'tip', 'warning', 'success'];

    const ICONS = {
        up: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" aria-hidden="true"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 15l7-7 7 7"/></svg>',
        down: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" aria-hidden="true"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M19 9l-7 7-7-7"/></svg>',
        trash: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" aria-hidden="true"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"/></svg>',
        plus: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" aria-hidden="true"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M12 4.5v15m7.5-7.5h-15"/></svg>',
        bold: '<svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true"><path d="M15.6 10.79c.97-.67 1.65-1.77 1.65-2.79 0-2.26-1.75-4-4-4H7v14h7.04c2.09 0 3.71-1.7 3.71-3.79 0-1.52-.86-2.82-2.15-3.42zM10 6.5h3c.83 0 1.5.67 1.5 1.5s-.67 1.5-1.5 1.5h-3v-3zm3.5 9H10v-3h3.5c.83 0 1.5.67 1.5 1.5s-.67 1.5-1.5 1.5z"/></svg>',
        italic: '<svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true"><path d="M10 4v3h2.21l-3.42 8H6v3h8v-3h-2.21l3.42-8H18V4z"/></svg>',
        link: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" aria-hidden="true"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13.828 10.172a4 4 0 00-5.656 0l-4 4a4 4 0 105.656 5.656l1.102-1.101m-.758-4.899a4 4 0 005.656 0l4-4a4 4 0 00-5.656-5.656l-1.1 1.1"/></svg>',
        image: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" aria-hidden="true"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z"/></svg>',
        upload: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" aria-hidden="true"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 8m4-4v12"/></svg>'
    };

    function element(tag, className, html) {
        const node = document.createElement(tag);
        if (className) node.className = className;
        if (html !== undefined && html !== null) node.innerHTML = html;
        return node;
    }

    function escapeHtml(value) {
        return String(value ?? '')
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;');
    }

    function toInt(value, fallback) {
        const parsed = Number.parseInt(value, 10);
        return Number.isFinite(parsed) && parsed > 0 ? parsed : fallback;
    }

    function plainTextFromHtml(html) {
        const container = document.createElement('div');
        container.innerHTML = String(html ?? '');
        return (container.textContent || '').replace(/\u00a0/g, ' ').replace(/\s+/g, ' ').trim();
    }

    function isSafeHref(value) {
        const href = String(value ?? '').trim();
        if (!href) return false;
        if (href.startsWith('//') || href.includes('\\')) return false;
        if (href.startsWith('/') || href.startsWith('#')) return true;

        try {
            const url = new URL(href, window.location.origin);
            return LINK_SCHEMES.has(url.protocol) && !url.username && !url.password;
        } catch {
            return false;
        }
    }

    /**
     * Deja únicamente el subconjunto inline que el servidor acepta para RichText.
     * No sustituye la sanitización del servidor; solo evita inyectar markup activo
     * mientras se edita o se pega contenido.
     */
    function sanitizeInline(html) {
        const source = String(html ?? '');
        if (!source.trim()) return '';

        const documentFragment = new DOMParser().parseFromString(
            `<body>${source}</body>`, 'text/html');
        if (!documentFragment.body) return '';

        cleanInlineChildren(documentFragment.body);
        return documentFragment.body.innerHTML;
    }

    function cleanInlineChildren(parent) {
        for (const node of Array.from(parent.childNodes)) {
            if (node.nodeType === Node.TEXT_NODE) continue;
            if (node.nodeType !== Node.ELEMENT_NODE) {
                node.remove();
                continue;
            }

            const tag = node.tagName.toLowerCase();
            if (DANGEROUS_TAGS.has(tag)) {
                node.remove();
                continue;
            }

            if (!ALLOWED_INLINE_TAGS.has(tag)) {
                cleanInlineChildren(node);
                while (node.firstChild) parent.insertBefore(node.firstChild, node);
                node.remove();
                continue;
            }

            if (tag === 'a') {
                const href = node.getAttribute('href') || '';
                for (const attribute of Array.from(node.attributes)) {
                    if (!['href', 'title'].includes(attribute.name.toLowerCase())) {
                        node.removeAttribute(attribute.name);
                    }
                }

                if (href && isSafeHref(href)) {
                    node.setAttribute('href', href);
                    if (/^https?:/i.test(href)) {
                        node.setAttribute('target', '_blank');
                        node.setAttribute('rel', 'noopener noreferrer');
                    } else {
                        node.removeAttribute('target');
                        node.removeAttribute('rel');
                    }
                } else {
                    node.removeAttribute('href');
                    node.removeAttribute('target');
                    node.removeAttribute('rel');
                }
            } else {
                for (const attribute of Array.from(node.attributes)) {
                    node.removeAttribute(attribute.name);
                }
            }

            cleanInlineChildren(node);
        }
    }

    function defaultsFor(type) {
        switch (type) {
            case 'heading':
                return { level: 2, text: '' };
            case 'ordered-list':
            case 'bullet-list':
                return { items: [''] };
            case 'quote':
                return { text: '', attribution: null };
            case 'callout':
                return { variant: 'info', title: null, text: '' };
            case 'image':
                return {
                    imageId: null, url: null, alt: '', caption: null, credit: null,
                    width: null, height: null
                };
            case 'table':
                return {
                    headers: ['Columna 1', 'Columna 2', 'Columna 3'],
                    rows: [['', '', '']],
                    caption: null
                };
            case 'code':
                return { code: '', language: null };
            case 'divider':
                return {};
            default:
                return { text: '' };
        }
    }

    class ContentBlocksEditor {
        constructor(root, input) {
            this.root = root;
            this.input = input;
            this.canvas = root.querySelector('[data-cbe-canvas]');
            this.toolbar = root.querySelector('[data-cbe-toolbar]');
            this.blocks = [];
            this.activeEditable = null;
            this.inlineToolbar = null;
            this.hideTimer = null;
            this.selectionTimer = null;
            this.noticeTimer = null;
            this.selectedBlock = null;
            this.inserterMenu = null;
            this.inserterMenuCleanup = null;

            this.allowedImageHosts = (root.dataset.allowedImageHosts || '')
                .split(',').map(host => host.trim().toLowerCase()).filter(Boolean);
            this.uploadUrl = root.dataset.uploadUrl || '/admin/articulos/upload-inline';
            this.articleId = root.dataset.articleId || '';
            this.maxBlocks = toInt(root.dataset.maxBlocks, 100);
            this.maxListItems = toInt(root.dataset.maxListItems, 50);
            this.maxTableRows = toInt(root.dataset.maxTableRows, 50);
            this.maxTableColumns = toInt(root.dataset.maxTableColumns, 10);
            this.maxUrlLength = toInt(root.dataset.maxUrlLength, 2048);
        }

        init() {
            if (!this.canvas) return;

            this.buildToolbar();
            this.inlineToolbar = this.createInlineToolbar();
            this.root.appendChild(this.inlineToolbar);

            const initial = this.parseInitial();
            if (initial.length === 0) {
                this.addBlock('paragraph', defaultsFor('paragraph'), false, false);
            } else {
                initial.forEach(item => this.addBlock(item.type, item, false, false));
            }

            this.sync();
            const form = this.input.closest('form');
            form?.addEventListener('submit', () => this.sync());

            document.addEventListener('selectionchange', () => this.handleSelectionChange());

            document.addEventListener('pointerdown', event => {
                if (this.root.contains(event.target)) return;
                if (this.inserterMenu?.contains(event.target)) return;
                this.clearSelectedBlock();
                this.closeInserterMenu();
            });

            this.canvas.addEventListener('pointerdown', event => {
                if (event.target === this.canvas) this.clearSelectedBlock();
            });
        }

        buildToolbar() {
            if (!this.toolbar) return;

            this.toolbar.innerHTML = '';
            const label = element('span', 'cbe-toolbar-label', 'Añadir bloque');
            const select = element('select', 'cbe-select cbe-add-select');
            select.setAttribute('aria-label', 'Tipo de bloque que se va a añadir');
            BLOCK_TYPES.forEach(item => {
                const option = document.createElement('option');
                option.value = item.type;
                option.textContent = item.label;
                select.appendChild(option);
            });

            const addButton = element('button', 'cbe-btn cbe-btn-primary', 'Añadir');
            addButton.type = 'button';
            addButton.addEventListener('click', () => {
                this.addBlock(select.value, defaultsFor(select.value));
            });

            this.status = element('span', 'cbe-status');
            this.status.setAttribute('role', 'status');
            this.status.setAttribute('aria-live', 'polite');

            this.toolbar.append(label, select, addButton, this.status);
        }

        createInlineToolbar() {
            const bar = element('div', 'cbe-inline-toolbar');
            bar.hidden = true;
            bar.innerHTML = `
                <button type="button" class="cbe-format-btn" data-cmd="bold" title="Negrita" aria-label="Negrita">${ICONS.bold}</button>
                <button type="button" class="cbe-format-btn" data-cmd="italic" title="Cursiva" aria-label="Cursiva">${ICONS.italic}</button>
                <button type="button" class="cbe-format-btn" data-cmd="createLink" title="Insertar enlace" aria-label="Insertar enlace">${ICONS.link}</button>
            `;

            bar.querySelectorAll('button[data-cmd]').forEach(button => {
                button.addEventListener('mousedown', event => {
                    event.preventDefault();
                    this.applyInlineCommand(button.dataset.cmd);
                });
            });

            bar.addEventListener('pointerdown', event => event.preventDefault());
            return bar;
        }

        parseInitial() {
            const raw = (this.input.value || '').trim();
            if (!raw) return [];

            try {
                const parsed = JSON.parse(raw);
                if (!Array.isArray(parsed)) {
                    this.notify('El cuerpo guardado no es una lista de bloques.');
                    return [];
                }

                return parsed.filter(item =>
                    item && typeof item === 'object' && TYPE_LABEL[item.type]);
            } catch (error) {
                console.warn('No se pudo leer el JSON del cuerpo editorial', error);
                this.notify('El cuerpo guardado no es un JSON válido. Se ha iniciado un editor vacío.');
                return [];
            }
        }

        addBlock(type, data, focus = true, runSync = true) {
            return this.insertBlockAt(this.blocks.length, type, data, focus, runSync);
        }

        insertBlockAt(index, type, data, focus = true, runSync = true) {
            if (this.blocks.length >= this.maxBlocks) {
                this.notify(`El cuerpo no puede superar los ${this.maxBlocks} bloques.`);
                return null;
            }

            const block = this.createBlock(type, data);
            const target = Math.max(0, Math.min(index, this.blocks.length));
            this.blocks.splice(target, 0, block);
            this.renderCanvas();

            if (runSync) this.sync();
            if (focus) {
                this.focusFirstField(block.el);
                this.selectBlock(block);
            }
            return block;
        }

        createBlockInserter(index) {
            const row = element('div', 'cbe-block-inserter');
            const lineBefore = element('span', 'cbe-inserter-line');
            const button = element('button', 'cbe-inserter-btn', ICONS.plus);
            button.type = 'button';
            button.title = 'Insertar bloque aquí';
            button.setAttribute('aria-label', 'Insertar bloque aquí');
            button.addEventListener('click', event => {
                event.stopPropagation();
                this.openInserterMenu(button, index);
            });
            const lineAfter = element('span', 'cbe-inserter-line');

            row.append(lineBefore, button, lineAfter);
            return row;
        }

        openInserterMenu(anchor, index) {
            this.closeInserterMenu();

            const menu = element('div', 'cbe-inserter-menu');
            menu.setAttribute('role', 'menu');
            menu.setAttribute('aria-label', 'Insertar bloque');

            BLOCK_TYPES.forEach(item => {
                const option = element('button', 'cbe-inserter-item', escapeHtml(item.label));
                option.type = 'button';
                option.setAttribute('role', 'menuitem');
                option.addEventListener('click', () => {
                    this.closeInserterMenu();
                    this.insertBlockAt(index, item.type, defaultsFor(item.type));
                });
                menu.appendChild(option);
            });

            document.body.appendChild(menu);
            const rect = anchor.getBoundingClientRect();
            const width = menu.offsetWidth;
            const left = Math.min(
                Math.max(8, rect.left - width / 2),
                Math.max(8, window.innerWidth - width - 8));
            menu.style.left = `${left}px`;

            const estimatedHeight = menu.offsetHeight;
            const below = rect.bottom + 8;
            const top = below + estimatedHeight > window.innerHeight - 8
                ? Math.max(8, rect.top - estimatedHeight - 8)
                : below;
            menu.style.top = `${top}px`;

            const onPointerDown = event => {
                if (menu.contains(event.target) || anchor.contains(event.target)) return;
                this.closeInserterMenu();
            };
            const onKeyDown = event => {
                if (event.key !== 'Escape') return;
                this.closeInserterMenu();
                anchor.focus();
            };

            document.addEventListener('pointerdown', onPointerDown, true);
            document.addEventListener('keydown', onKeyDown);
            this.inserterMenuCleanup = () => {
                document.removeEventListener('pointerdown', onPointerDown, true);
                document.removeEventListener('keydown', onKeyDown);
            };
            this.inserterMenu = menu;
            menu.querySelector('button')?.focus();
        }

        closeInserterMenu() {
            if (this.inserterMenuCleanup) {
                this.inserterMenuCleanup();
                this.inserterMenuCleanup = null;
            }
            this.inserterMenu?.remove();
            this.inserterMenu = null;
        }

        renderCanvas() {
            if (!this.canvas) return;
            this.canvas.innerHTML = '';

            this.blocks.forEach((block, index) => {
                this.canvas.appendChild(block.el);
                this.canvas.appendChild(this.createBlockInserter(index + 1));
            });

            this.refreshIndices();
        }

        selectBlock(block) {
            if (!block || this.selectedBlock === block) return;
            this.selectedBlock?.el.classList.remove('is-selected');
            this.selectedBlock = block;
            block.el.classList.add('is-selected');
        }

        clearSelectedBlock() {
            this.selectedBlock?.el.classList.remove('is-selected');
            this.selectedBlock = null;
        }

        createBlock(type, data) {
            const block = { type, el: null };
            const normalized = this.normaliseData(type, data);
            const card = element('article', 'cbe-block');
            card.dataset.type = type;

            const header = this.buildBlockHeader(block, type);
            const body = this.buildBlockBody(block, type, normalized);
            card.append(header, body);
            block.el = card;

            card.addEventListener('pointerdown', () => this.selectBlock(block));
            card.addEventListener('focusin', () => this.selectBlock(block));
            return block;
        }

        buildBlockHeader(block, type) {
            const header = element('div', 'cbe-block-header cbe-block-toolbar');
            const position = element('span', 'cbe-block-index');
            position.textContent = '1';
            const typeLabel = element('span', 'cbe-block-type', TYPE_LABEL[type] || 'Bloque');

            const select = element('select', 'cbe-select cbe-type-select');
            select.setAttribute('aria-label', 'Cambiar tipo de bloque');
            BLOCK_TYPES.forEach(item => {
                const option = document.createElement('option');
                option.value = item.type;
                option.textContent = item.label;
                option.selected = item.type === type;
                select.appendChild(option);
            });
            select.addEventListener('change', () => {
                if (select.value !== block.type) this.changeType(block, select.value);
            });

            const formatGroup = this.buildBlockFormatActions(block, type);

            const actions = element('div', 'cbe-block-actions');
            actions.append(
                this.iconButton('Mover bloque arriba', ICONS.up, () => this.moveBlock(block, -1)),
                this.iconButton('Mover bloque abajo', ICONS.down, () => this.moveBlock(block, 1)),
                this.iconButton('Eliminar bloque', ICONS.trash, () => this.removeBlock(block), 'cbe-icon-btn-danger')
            );

            header.append(position, typeLabel, select);
            if (formatGroup) header.appendChild(formatGroup);
            header.appendChild(actions);
            return header;
        }

        buildBlockFormatActions(block, type) {
            if (!['paragraph', 'quote', 'callout', 'ordered-list', 'bullet-list'].includes(type)) {
                return null;
            }

            const group = element('div', 'cbe-block-format');
            const formats = [
                ['bold', ICONS.bold, 'Negrita'],
                ['italic', ICONS.italic, 'Cursiva'],
                ['createLink', ICONS.link, 'Insertar enlace']
            ];

            formats.forEach(([command, icon, label]) => {
                const button = element('button', 'cbe-format-btn', icon);
                button.type = 'button';
                button.title = label;
                button.setAttribute('aria-label', label);
                button.addEventListener('mousedown', event => {
                    event.preventDefault();
                    const field = this.richFieldForBlock(block);
                    if (field) this.applyInlineCommand(command, field, false);
                });
                group.appendChild(button);
            });

            return group;
        }

        richFieldForBlock(block) {
            if (this.activeEditable && block.el.contains(this.activeEditable)) {
                return this.activeEditable;
            }
            return block.el.querySelector('.cbe-rich');
        }

        buildBlockBody(block, type, data) {
            switch (type) {
                case 'heading':
                    return this.buildHeadingBody(data);
                case 'ordered-list':
                case 'bullet-list':
                    return this.buildListBody(block, type, data);
                case 'quote':
                    return this.buildQuoteBody(data);
                case 'callout':
                    return this.buildCalloutBody(data);
                case 'image':
                    return this.buildImageBody(data);
                case 'table':
                    return this.buildTableBody(data);
                case 'code':
                    return this.buildCodeBody(data);
                case 'divider':
                    return this.buildDividerBody();
                default:
                    return this.buildParagraphBody(data);
            }
        }

        buildParagraphBody(data) {
            const body = element('div', 'cbe-block-body');
            body.appendChild(this.createRich(data.text, 'Escribe un párrafo...', () => {
                document.execCommand('insertLineBreak');
            }));
            return body;
        }

        buildHeadingBody(data) {
            const body = element('div', 'cbe-block-body');
            const row = element('div', 'cbe-row cbe-row-heading');

            const level = element('select', 'cbe-select cbe-heading-level');
            level.dataset.field = 'level';
            level.setAttribute('aria-label', 'Nivel del encabezado');
            [2, 3].forEach(value => {
                const option = document.createElement('option');
                option.value = String(value);
                option.textContent = `H${value}`;
                option.selected = Number(data.level) === value;
                level.appendChild(option);
            });
            level.addEventListener('change', () => this.sync());

            const text = this.createPlainInput(data.text, 'Escribe un encabezado...', 'text');
            text.classList.add('cbe-heading-input');
            text.setAttribute('aria-label', 'Texto del encabezado');

            row.append(level, text);
            body.appendChild(row);
            return body;
        }

        buildListBody(block, type, data) {
            const body = element('div', 'cbe-block-body');
            const list = element(type === 'ordered-list' ? 'ol' : 'ul', 'cbe-list');
            list.dataset.field = 'items';

            const items = Array.isArray(data.items) && data.items.length > 0 ? data.items : [''];
            items.forEach(item => this.appendListItem(list, item, false));

            const controls = element('div', 'cbe-list-controls');
            const add = element('button', 'cbe-btn cbe-btn-small', `${ICONS.plus}<span>Añadir elemento</span>`);
            add.type = 'button';
            add.addEventListener('click', () => {
                const item = this.appendListItem(list, '', true);
                if (item) this.sync();
            });

            const remove = element('button', 'cbe-btn cbe-btn-small cbe-btn-muted', 'Quitar último');
            remove.type = 'button';
            remove.addEventListener('click', () => {
                if (list.children.length > 1) {
                    list.lastElementChild.remove();
                } else if (list.firstElementChild) {
                    const rich = list.firstElementChild.querySelector('.cbe-rich');
                    if (rich) rich.innerHTML = '';
                }
                this.sync();
            });

            controls.append(add, remove);
            body.append(list, controls);
            return body;
        }

        appendListItem(list, value, focus) {
            if (list.children.length >= this.maxListItems) {
                this.notify(`La lista no puede superar los ${this.maxListItems} elementos.`);
                return null;
            }

            const item = element('li', 'cbe-list-item');
            const rich = this.createRich(value, 'Elemento de la lista...', () => {
                const next = this.appendListItem(list, '', true);
                if (next) this.sync();
            });
            rich.dataset.field = 'item';

            const remove = this.iconButton('Eliminar elemento', ICONS.trash, () => {
                if (list.children.length > 1) {
                    item.remove();
                } else {
                    rich.innerHTML = '';
                }
                this.sync();
            }, 'cbe-icon-btn-danger cbe-list-remove');
            remove.type = 'button';

            item.append(rich, remove);
            list.appendChild(item);
            if (focus) rich.focus();
            return item;
        }

        buildQuoteBody(data) {
            const body = element('div', 'cbe-block-body cbe-quote');
            const text = this.createRich(data.text, 'Escribe la cita...', () => {
                document.execCommand('insertLineBreak');
            });
            text.classList.add('cbe-quote-text');

            const attribution = this.createPlainInput(
                data.attribution || '', 'Autor o fuente de la cita', 'attribution');
            attribution.classList.add('cbe-quote-attribution');

            body.append(text, attribution);
            return body;
        }

        buildCalloutBody(data) {
            const body = element('div', 'cbe-block-body cbe-callout');
            body.dataset.variant = CALLOUT_VARIANTS.includes(data.variant) ? data.variant : 'info';

            const row = element('div', 'cbe-row');
            const variant = element('select', 'cbe-select');
            variant.dataset.field = 'variant';
            variant.setAttribute('aria-label', 'Variante del callout');
            [['info', 'Información'], ['tip', 'Consejo'], ['warning', 'Atención'], ['success', 'Confirmación']]
                .forEach(([value, label]) => {
                    const option = document.createElement('option');
                    option.value = value;
                    option.textContent = label;
                    option.selected = body.dataset.variant === value;
                    variant.appendChild(option);
                });
            variant.addEventListener('change', () => {
                body.dataset.variant = variant.value;
                this.sync();
            });

            const title = this.createPlainInput(data.title || '', 'Título opcional', 'title');
            row.append(this.fieldGroup('Variante', variant), this.fieldGroup('Título', title));

            const text = this.createRich(data.text, 'Mensaje o dato clave...', () => {
                document.execCommand('insertLineBreak');
            });

            body.append(row, this.fieldGroup('Contenido', text));
            return body;
        }

        buildImageBody(data) {
            const body = element('div', 'cbe-block-body cbe-image-body');

            const preview = element('div', 'cbe-image-preview');
            const image = document.createElement('img');
            image.alt = 'Vista previa de la imagen';
            image.loading = 'lazy';
            const empty = element('span', 'cbe-image-empty', 'Sin imagen seleccionada');
            preview.append(image, empty);

            const imageId = document.createElement('input');
            imageId.type = 'hidden';
            imageId.dataset.field = 'imageId';
            imageId.value = data.imageId || '';

            const width = document.createElement('input');
            width.type = 'hidden';
            width.dataset.field = 'width';
            width.value = data.width || '';

            const height = document.createElement('input');
            height.type = 'hidden';
            height.dataset.field = 'height';
            height.value = data.height || '';

            const url = this.createPlainInput(
                data.url || '', 'https://... o /uploads/...', 'url');
            url.classList.add('cbe-mono');

            const alt = this.createPlainInput(
                data.alt || '', 'Descripción para accesibilidad', 'alt');
            const caption = this.createPlainInput(
                data.caption || '', 'Pie de foto (opcional)', 'caption');
            const credit = this.createPlainInput(
                data.credit || '', 'Crédito o agencia (opcional)', 'credit');

            const actions = element('div', 'cbe-image-actions');
            const library = element('button', 'cbe-btn cbe-btn-small cbe-btn-primary',
                `${ICONS.image}<span>Biblioteca</span>`);
            library.type = 'button';
            library.dataset.imageLibrary = 'true';

            const upload = element('button', 'cbe-btn cbe-btn-small',
                `${ICONS.upload}<span>Subir</span>`);
            upload.type = 'button';
            upload.dataset.imageUpload = 'true';

            const clear = element('button', 'cbe-btn cbe-btn-small cbe-btn-muted', 'Limpiar');
            clear.type = 'button';
            clear.dataset.imageClear = 'true';

            const file = document.createElement('input');
            file.type = 'file';
            file.accept = 'image/*';
            file.hidden = true;
            file.dataset.field = 'file';

            const error = element('p', 'cbe-error');
            error.setAttribute('role', 'alert');

            actions.append(library, upload, clear, file);
            const row = element('div', 'cbe-row cbe-row-3');
            row.append(
                this.fieldGroup('Texto alternativo', alt),
                this.fieldGroup('Pie de foto', caption),
                this.fieldGroup('Crédito', credit));

            body.append(preview, this.fieldGroup('URL de la imagen', url), row, actions, error);
            body.append(imageId, width, height);

            this.bindImageEvents(body);
            this.updateImagePreview(body);
            this.validateImage(body);
            return body;
        }

        bindImageEvents(body) {
            const url = body.querySelector('[data-field="url"]');
            const imageId = body.querySelector('[data-field="imageId"]');
            const alt = body.querySelector('[data-field="alt"]');
            const caption = body.querySelector('[data-field="caption"]');
            const credit = body.querySelector('[data-field="credit"]');
            const file = body.querySelector('[data-field="file"]');
            const upload = body.querySelector('[data-image-upload]');
            const library = body.querySelector('[data-image-library]');
            const clear = body.querySelector('[data-image-clear]');

            url?.addEventListener('input', () => {
                if (imageId) imageId.value = '';
                delete body.dataset.preview;
                this.updateImagePreview(body);
                this.validateImage(body);
                this.sync();
            });

            alt?.addEventListener('input', () => {
                this.validateImage(body);
                this.sync();
            });
            caption?.addEventListener('input', () => this.sync());
            credit?.addEventListener('input', () => this.sync());

            library?.addEventListener('click', () => {
                this.openMediaLibrary(selected => this.applyLibraryImage(body, selected));
            });

            upload?.addEventListener('click', () => file?.click());

            file?.addEventListener('change', async () => {
                if (!file.files || file.files.length === 0) return;
                const selected = file.files[0];
                file.value = '';
                await this.uploadImage(body, selected);
            });

            clear?.addEventListener('click', () => {
                if (url) url.value = '';
                if (imageId) imageId.value = '';
                const width = body.querySelector('[data-field="width"]');
                const height = body.querySelector('[data-field="height"]');
                if (width) width.value = '';
                if (height) height.value = '';
                delete body.dataset.preview;
                this.updateImagePreview(body);
                this.validateImage(body);
                this.sync();
            });
        }

        updateImagePreview(body) {
            const urlInput = body.querySelector('[data-field="url"]');
            const preview = body.querySelector('.cbe-image-preview');
            const image = preview?.querySelector('img');
            if (!preview || !image) return;

            const url = (urlInput?.value || '').trim() || body.dataset.preview || '';
            const canPreview = this.isPreviewUrl(url);

            if (canPreview) {
                image.src = url;
                preview.classList.add('has-image');
            } else {
                image.removeAttribute('src');
                preview.classList.remove('has-image');
            }
        }

        validateImage(body) {
            const urlInput = body.querySelector('[data-field="url"]');
            const imageId = body.querySelector('[data-field="imageId"]')?.value || '';
            const alt = body.querySelector('[data-field="alt"]');
            const error = body.querySelector('.cbe-error');
            const url = (urlInput?.value || '').trim();
            const messages = [];

            if (!imageId && !url) {
                messages.push('Selecciona una imagen de la biblioteca o indica una URL.');
            } else if (url && !this.isAllowedImageUrl(url)) {
                messages.push('La URL debe ser relativa o usar HTTPS y un host permitido.');
            }

            if (!alt?.value.trim()) {
                messages.push('El texto alternativo es obligatorio.');
            }

            if (urlInput) urlInput.classList.toggle('cbe-invalid', !!url && !this.isAllowedImageUrl(url));
            alt?.classList.toggle('cbe-invalid', !alt?.value.trim());

            if (error) {
                error.textContent = messages.join(' ');
                error.classList.toggle('is-visible', messages.length > 0);
            }
        }

        applyLibraryImage(body, selected) {
            if (!selected) return;

            const urlInput = body.querySelector('[data-field="url"]');
            const imageId = body.querySelector('[data-field="imageId"]');
            const alt = body.querySelector('[data-field="alt"]');
            const width = body.querySelector('[data-field="width"]');
            const height = body.querySelector('[data-field="height"]');
            const candidate = String(selected.url || '').trim();

            if (imageId) imageId.value = selected.id || '';
            if (urlInput) {
                urlInput.value = candidate && this.isAllowedImageUrl(candidate) ? candidate : '';
            }

            if (alt && !alt.value.trim()) {
                alt.value = selected.alt || selected.fileName || '';
            }
            if (width) width.value = selected.width || '';
            if (height) height.value = selected.height || '';

            const previewUrl = selected.thumbnailUrl || selected.url || '';
            if (previewUrl) body.dataset.preview = previewUrl;
            else delete body.dataset.preview;

            this.updateImagePreview(body);
            this.validateImage(body);
            this.sync();
        }

        async uploadImage(body, file) {
            const formData = new FormData();
            formData.append('file', file);
            if (this.articleId) formData.append('articleId', this.articleId);

            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            const button = body.querySelector('[data-image-upload]');
            const original = button?.innerHTML;
            if (button) {
                button.disabled = true;
                button.textContent = 'Subiendo...';
            }

            try {
                const response = await fetch(this.uploadUrl, {
                    method: 'POST',
                    headers: token ? { RequestVerificationToken: token } : {},
                    body: formData
                });

                const data = await response.json().catch(() => null);
                if (!response.ok) {
                    this.notify(data?.error || 'No se pudo subir la imagen.');
                    return;
                }

                if (data?.id) {
                    this.applyLibraryImage(body, {
                        id: data.id,
                        url: data.url,
                        alt: data.alt,
                        fileName: file.name,
                        width: data.width,
                        height: data.height
                    });
                } else if (data?.error) {
                    this.notify(data.error);
                }
            } catch (error) {
                console.error('Error subiendo imagen del bloque', error);
                this.notify('No se pudo subir la imagen del bloque.');
            } finally {
                if (button) {
                    button.disabled = false;
                    button.innerHTML = original;
                }
            }
        }

        buildTableBody(data) {
            const body = element('div', 'cbe-block-body cbe-table-body');
            const controls = element('div', 'cbe-table-controls');
            const addRow = element('button', 'cbe-btn cbe-btn-small', '+ Fila');
            const removeRow = element('button', 'cbe-btn cbe-btn-small cbe-btn-muted', '− Fila');
            const addColumn = element('button', 'cbe-btn cbe-btn-small', '+ Columna');
            const removeColumn = element('button', 'cbe-btn cbe-btn-small cbe-btn-muted', '− Columna');
            [addRow, removeRow, addColumn, removeColumn].forEach(button => button.type = 'button');
            controls.append(addRow, removeRow, addColumn, removeColumn);

            const wrapper = element('div', 'cbe-table-wrap');
            const table = element('table', 'cbe-table');
            const thead = document.createElement('thead');
            const headerRow = document.createElement('tr');
            const tbody = document.createElement('tbody');

            const headers = Array.isArray(data.headers) && data.headers.length > 0
                ? data.headers : ['Columna 1'];
            headers.forEach(header => headerRow.appendChild(this.createTableCell(header, true)));
            thead.appendChild(headerRow);

            const rows = Array.isArray(data.rows) ? data.rows : [];
            rows.forEach(row => {
                const tr = document.createElement('tr');
                for (let index = 0; index < headers.length; index += 1) {
                    tr.appendChild(this.createTableCell(row?.[index] || '', false));
                }
                tbody.appendChild(tr);
            });

            table.append(thead, tbody);
            wrapper.appendChild(table);

            const caption = this.createPlainInput(
                data.caption || '', 'Título o pie de la tabla (opcional)', 'caption');

            addRow.addEventListener('click', () => {
                if (tbody.rows.length >= this.maxTableRows - 1) {
                    this.notify(`La tabla no puede superar las ${this.maxTableRows} filas incluida la cabecera.`);
                    return;
                }
                const count = headerRow.children.length;
                const tr = document.createElement('tr');
                for (let index = 0; index < count; index += 1) {
                    tr.appendChild(this.createTableCell('', false));
                }
                tbody.appendChild(tr);
                this.sync();
                tr.querySelector('input')?.focus();
            });

            removeRow.addEventListener('click', () => {
                if (tbody.rows.length > 0) {
                    tbody.deleteRow(-1);
                    this.sync();
                }
            });

            addColumn.addEventListener('click', () => {
                if (headerRow.children.length >= this.maxTableColumns) {
                    this.notify(`La tabla no puede superar las ${this.maxTableColumns} columnas.`);
                    return;
                }
                headerRow.appendChild(this.createTableCell('', true));
                Array.from(tbody.rows).forEach(row => {
                    row.appendChild(this.createTableCell('', false));
                });
                this.sync();
            });

            removeColumn.addEventListener('click', () => {
                if (headerRow.children.length <= 1) return;
                headerRow.lastElementChild?.remove();
                Array.from(tbody.rows).forEach(row => row.lastElementChild?.remove());
                this.sync();
            });

            body.append(controls, wrapper, this.fieldGroup('Pie de tabla', caption));
            return body;
        }

        createTableCell(value, isHeader) {
            const cell = document.createElement(isHeader ? 'th' : 'td');
            const input = document.createElement('input');
            input.type = 'text';
            input.className = 'cbe-table-input';
            input.dataset.field = isHeader ? 'header' : 'cell';
            input.value = value || '';
            input.placeholder = isHeader ? 'Columna' : 'Dato';
            input.addEventListener('input', () => this.sync());
            cell.appendChild(input);
            return cell;
        }

        buildCodeBody(data) {
            const body = element('div', 'cbe-block-body cbe-code-body');
            const code = document.createElement('textarea');
            code.className = 'cbe-code';
            code.rows = 7;
            code.spellcheck = false;
            code.dataset.field = 'code';
            code.value = data.code || '';
            code.placeholder = 'Pega o escribe el código...';
            code.addEventListener('input', () => this.sync());

            const language = this.createPlainInput(
                data.language || '', 'csharp, javascript, json...', 'language');

            body.append(
                this.fieldGroup('Código', code),
                this.fieldGroup('Lenguaje (opcional)', language));
            return body;
        }

        buildDividerBody() {
            const body = element('div', 'cbe-block-body cbe-divider-body');
            body.innerHTML = '<span class="cbe-divider-line"></span><span>Separador visual</span><span class="cbe-divider-line"></span>';
            return body;
        }

        createRich(value, placeholder, onEnter) {
            const rich = element('div', 'cbe-rich');
            rich.contentEditable = 'true';
            rich.dataset.field = 'text';
            rich.dataset.placeholder = placeholder;
            rich.setAttribute('role', 'textbox');
            rich.setAttribute('aria-multiline', 'true');
            rich.innerHTML = sanitizeInline(value);

            rich.addEventListener('input', () => this.sync());
            rich.addEventListener('focus', () => {
                this.activeEditable = rich;
            });
            rich.addEventListener('blur', () => this.scheduleHideInlineToolbar());
            rich.addEventListener('paste', event => {
                const html = event.clipboardData?.getData('text/html');
                const text = event.clipboardData?.getData('text/plain');
                if (html) {
                    event.preventDefault();
                    document.execCommand('insertHTML', false, sanitizeInline(html));
                } else if (text) {
                    event.preventDefault();
                    document.execCommand('insertText', false, text);
                }
                this.sync();
            });

            if (onEnter) {
                rich.addEventListener('keydown', event => {
                    if (event.key === 'Enter' && !event.shiftKey) {
                        event.preventDefault();
                        onEnter();
                        this.sync();
                    }
                });
            }

            return rich;
        }

        createPlainInput(value, placeholder, field) {
            const input = element('input', 'cbe-input');
            input.type = 'text';
            input.value = value || '';
            input.placeholder = placeholder || '';
            input.dataset.field = field;
            input.autocomplete = 'off';
            input.addEventListener('input', () => this.sync());
            return input;
        }

        fieldGroup(label, control) {
            const wrapper = element('div', 'cbe-field');
            const text = element('span', 'cbe-label', escapeHtml(label));
            control.setAttribute('aria-label', label);
            wrapper.append(text, control);
            return wrapper;
        }

        iconButton(label, icon, handler, extraClass = '') {
            const button = element('button', `cbe-icon-btn ${extraClass}`.trim(), icon);
            button.type = 'button';
            button.title = label;
            button.setAttribute('aria-label', label);
            button.addEventListener('click', event => {
                event.preventDefault();
                event.stopPropagation();
                handler();
            });
            return button;
        }

        handleSelectionChange() {
            clearTimeout(this.selectionTimer);
            this.selectionTimer = setTimeout(() => {
                const selection = document.getSelection();
                if (!selection || selection.isCollapsed || selection.rangeCount === 0) {
                    this.scheduleHideInlineToolbar();
                    return;
                }

                const container = selection.getRangeAt(0).commonAncestorContainer;
                const element = container.nodeType === Node.ELEMENT_NODE
                    ? container
                    : container.parentElement;
                const field = element?.closest('.cbe-rich');
                if (!field || !this.root.contains(field)) {
                    this.scheduleHideInlineToolbar();
                    return;
                }

                this.showInlineToolbar(field);
            }, 10);
        }

        showInlineToolbar(field) {
            this.activeEditable = field;
            if (!this.inlineToolbar) return;

            this.root.classList.add('cbe-bubble-open');
            const rect = field.getBoundingClientRect();
            this.inlineToolbar.hidden = false;
            const top = rect.top < 60 ? rect.bottom + 6 : rect.top - 42;
            const left = Math.min(Math.max(8, rect.left), Math.max(8, window.innerWidth - 180));
            this.inlineToolbar.style.top = `${top}px`;
            this.inlineToolbar.style.left = `${left}px`;
        }

        scheduleHideInlineToolbar() {
            clearTimeout(this.hideTimer);
            this.hideTimer = setTimeout(() => {
                if (!this.inlineToolbar) return;
                if (this.inlineToolbar.contains(document.activeElement)) return;
                this.inlineToolbar.hidden = true;
                this.root.classList.remove('cbe-bubble-open');
                this.activeEditable = null;
            }, 180);
        }

        applyInlineCommand(command, field = this.activeEditable, showBubble = true) {
            if (!field) return;
            field.focus();

            if (command === 'createLink') {
                const url = window.prompt('URL del enlace (https://, mailto: o ruta interna):', 'https://');
                if (!url) return;
                if (!isSafeHref(url)) {
                    this.notify('El enlace debe usar http, https, mailto o una ruta interna.');
                    return;
                }
                document.execCommand('createLink', false, url);
            } else {
                document.execCommand(command, false, null);
            }

            this.sync();
            if (showBubble) this.showInlineToolbar(field);
        }

        moveBlock(block, delta) {
            const from = this.blocks.indexOf(block);
            const to = from + delta;
            if (from < 0 || to < 0 || to >= this.blocks.length) return;

            this.blocks.splice(from, 1);
            this.blocks.splice(to, 0, block);
            this.renderCanvas();
            this.sync();
        }

        removeBlock(block) {
            const index = this.blocks.indexOf(block);
            if (index < 0) return;

            if (this.selectedBlock === block) this.clearSelectedBlock();
            this.blocks.splice(index, 1);
            if (this.blocks.length === 0) {
                this.addBlock('paragraph', defaultsFor('paragraph'), false, false);
            } else {
                this.renderCanvas();
            }
            this.sync();
        }

        changeType(block, newType) {
            if (!TYPE_LABEL[newType] || block.type === newType) return;

            const wasSelected = this.selectedBlock === block;
            const oldData = this.readBlock(block);
            const newData = this.convertData(oldData, newType);
            const index = this.blocks.indexOf(block);
            const newBlock = this.createBlock(newType, newData);

            block.el.replaceWith(newBlock.el);
            if (index >= 0) this.blocks[index] = newBlock;
            if (wasSelected) {
                this.selectedBlock = newBlock;
                newBlock.el.classList.add('is-selected');
            }
            this.refreshIndices();
            this.sync();
        }

        convertData(oldData, newType) {
            const richText = oldData.text
                || (Array.isArray(oldData.items) ? oldData.items.join('<br>') : '')
                || oldData.code
                || '';
            const plain = plainTextFromHtml(richText) || oldData.alt || '';

            switch (newType) {
                case 'paragraph':
                    return { type: newType, text: richText };
                case 'heading':
                    return {
                        type: newType,
                        level: oldData.level === 3 ? 3 : 2,
                        text: plainTextFromHtml(richText)
                    };
                case 'ordered-list':
                case 'bullet-list':
                    return {
                        type: newType,
                        items: Array.isArray(oldData.items) && oldData.items.length
                            ? oldData.items
                            : [richText || '']
                    };
                case 'quote':
                    return {
                        type: newType,
                        text: richText,
                        attribution: oldData.attribution || null
                    };
                case 'callout':
                    return {
                        type: newType,
                        variant: CALLOUT_VARIANTS.includes(oldData.variant) ? oldData.variant : 'info',
                        title: oldData.title || null,
                        text: richText
                    };
                case 'image':
                    return {
                        type: newType,
                        imageId: oldData.imageId || null,
                        url: oldData.url || null,
                        alt: oldData.alt || plain,
                        caption: oldData.caption || null,
                        credit: oldData.credit || null,
                        width: oldData.width || null,
                        height: oldData.height || null
                    };
                case 'table':
                    return Array.isArray(oldData.headers) && oldData.headers.length
                        ? {
                            type: newType,
                            headers: oldData.headers,
                            rows: oldData.rows || [],
                            caption: oldData.caption || null
                        }
                        : {
                            type: newType,
                            headers: ['Columna 1', 'Columna 2', 'Columna 3'],
                            rows: [['', '', '']],
                            caption: null
                        };
                case 'code':
                    return {
                        type: newType,
                        code: oldData.code || plain,
                        language: oldData.language || null
                    };
                case 'divider':
                    return { type: newType };
                default:
                    return defaultsFor(newType);
            }
        }

        readBlock(block) {
            const card = block.el;
            const type = block.type;
            const richText = selector =>
                sanitizeInline(card.querySelector(selector)?.innerHTML || '');
            const inputValue = selector =>
                (card.querySelector(selector)?.value || '').trim();

            switch (type) {
                case 'heading':
                    return {
                        type,
                        level: Number(inputValue('[data-field="level"]')) === 3 ? 3 : 2,
                        text: inputValue('[data-field="text"]')
                    };
                case 'ordered-list':
                case 'bullet-list':
                    return {
                        type,
                        items: Array.from(card.querySelectorAll('[data-field="items"] > li .cbe-rich'))
                            .map(item => sanitizeInline(item.innerHTML))
                            .filter(item => plainTextFromHtml(item).length > 0)
                    };
                case 'quote':
                    return {
                        type,
                        text: richText('[data-field="text"]'),
                        attribution: inputValue('[data-field="attribution"]') || null
                    };
                case 'callout':
                    return {
                        type,
                        variant: inputValue('[data-field="variant"]') || 'info',
                        title: inputValue('[data-field="title"]') || null,
                        text: richText('[data-field="text"]')
                    };
                case 'image':
                    return {
                        type,
                        imageId: inputValue('[data-field="imageId"]') || null,
                        url: inputValue('[data-field="url"]') || null,
                        alt: inputValue('[data-field="alt"]'),
                        caption: inputValue('[data-field="caption"]') || null,
                        credit: inputValue('[data-field="credit"]') || null,
                        width: this.optionalNumber(inputValue('[data-field="width"]')),
                        height: this.optionalNumber(inputValue('[data-field="height"]'))
                    };
                case 'table': {
                    const headers = Array.from(card.querySelectorAll('thead input'))
                        .map(input => input.value.trim());
                    const rows = Array.from(card.querySelectorAll('tbody tr')).map(row => {
                        const cells = Array.from(row.querySelectorAll('input'))
                            .map(input => input.value.trim());
                        while (cells.length < headers.length) cells.push('');
                        return cells.slice(0, headers.length);
                    });
                    return {
                        type,
                        headers,
                        rows,
                        caption: inputValue('[data-field="caption"]') || null
                    };
                }
                case 'code':
                    return {
                        type,
                        code: inputValue('[data-field="code"]'),
                        language: inputValue('[data-field="language"]') || null
                    };
                case 'divider':
                    return { type };
                default:
                    return { type: 'paragraph', text: richText('[data-field="text"]') };
            }
        }

        optionalNumber(value) {
            if (!value) return null;
            const parsed = Number.parseInt(value, 10);
            return Number.isFinite(parsed) && parsed > 0 ? parsed : null;
        }

        normaliseData(type, data) {
            const base = defaultsFor(type);
            const merged = Object.assign(base, data || {});

            if (type === 'heading') {
                merged.level = Number(merged.level) === 3 ? 3 : 2;
                merged.text = String(merged.text || '');
            } else if (type === 'ordered-list' || type === 'bullet-list') {
                merged.items = Array.isArray(merged.items) && merged.items.length
                    ? merged.items.slice(0, this.maxListItems).map(item => String(item ?? ''))
                    : [''];
            } else if (type === 'callout') {
                merged.variant = CALLOUT_VARIANTS.includes(merged.variant) ? merged.variant : 'info';
                merged.title = merged.title || null;
                merged.text = String(merged.text || '');
            } else if (type === 'image') {
                merged.imageId = merged.imageId || null;
                merged.url = merged.url || null;
                merged.alt = String(merged.alt || '');
                merged.caption = merged.caption || null;
                merged.credit = merged.credit || null;
                merged.width = Number.isFinite(Number(merged.width)) ? Number(merged.width) : null;
                merged.height = Number.isFinite(Number(merged.height)) ? Number(merged.height) : null;
            } else if (type === 'table') {
                merged.headers = Array.isArray(merged.headers) && merged.headers.length
                    ? merged.headers.slice(0, this.maxTableColumns).map(value => String(value ?? ''))
                    : ['Columna 1'];
                merged.rows = Array.isArray(merged.rows)
                    ? merged.rows.slice(0, Math.max(0, this.maxTableRows - 1)).map(row => {
                        const cells = Array.isArray(row) ? row.map(value => String(value ?? '')) : [];
                        while (cells.length < merged.headers.length) cells.push('');
                        return cells.slice(0, merged.headers.length);
                    })
                    : [];
                if (merged.rows.length === 0 && merged.headers.length > 0) {
                    merged.rows = [merged.headers.map(() => '')];
                }
                merged.caption = merged.caption || null;
            } else if (type === 'code') {
                merged.code = String(merged.code || '');
                merged.language = merged.language || null;
            } else if (type === 'paragraph') {
                merged.text = String(merged.text || '');
            } else if (type === 'quote') {
                merged.text = String(merged.text || '');
                merged.attribution = merged.attribution || null;
            }

            return merged;
        }

        refreshIndices() {
            this.blocks.forEach((block, index) => {
                block.el.dataset.index = String(index);
                const marker = block.el.querySelector('.cbe-block-index');
                if (marker) marker.textContent = String(index + 1);
            });
        }

        focusFirstField(card) {
            const field = card.querySelector(
                'input:not([type="hidden"]):not([type="file"]), textarea, [contenteditable="true"]');
            field?.focus();
        }

        sync() {
            if (!this.input) return;
            this.input.value = JSON.stringify(this.blocks.map(block => this.readBlock(block)));
        }

        isPreviewUrl(value) {
            return this.isAllowedImageUrl(value);
        }

        isAllowedImageUrl(value) {
            const url = String(value || '').trim();
            if (!url || url.length > this.maxUrlLength) return false;
            if (url.startsWith('//') || url.includes('\\')) return false;
            if (url.startsWith('/')) return true;

            try {
                const parsed = new URL(url);
                if (parsed.protocol !== 'https:') return false;
                if (parsed.username || parsed.password) return false;
                return this.allowedImageHosts.includes(parsed.hostname.toLowerCase());
            } catch {
                return false;
            }
        }

        notify(message) {
            if (!this.status) return;
            this.status.textContent = message;
            clearTimeout(this.noticeTimer);
            this.noticeTimer = setTimeout(() => {
                this.status.textContent = '';
            }, 4500);
        }

        openMediaLibrary(callback) {
            if (typeof window.openMediaLibraryModalForInline === 'function') {
                window.openMediaLibraryModalForInline(callback);
                return;
            }

            this.showFallbackMediaLibrary(callback);
        }

        async showFallbackMediaLibrary(callback) {
            const previousFocus = document.activeElement;
            const overlay = element('div', 'cbe-media-overlay');
            overlay.innerHTML = `
                <div class="cbe-media-dialog" role="dialog" aria-modal="true" aria-labelledby="cbeMediaTitle">
                    <div class="cbe-media-header">
                        <h3 id="cbeMediaTitle">Biblioteca de imágenes</h3>
                        <button type="button" class="cbe-icon-btn" data-media-close aria-label="Cerrar">×</button>
                    </div>
                    <div class="cbe-media-search">
                        <input type="search" class="cbe-input" data-media-search placeholder="Buscar por nombre..." />
                    </div>
                    <div class="cbe-media-grid" data-media-grid></div>
                    <div class="cbe-media-footer">
                        <span class="cbe-media-status" data-media-status>Selecciona una imagen.</span>
                        <button type="button" class="cbe-btn cbe-btn-muted" data-media-cancel>Cancelar</button>
                        <button type="button" class="cbe-btn cbe-btn-primary" data-media-confirm disabled>Insertar</button>
                    </div>
                </div>
            `;
            document.body.appendChild(overlay);
            document.body.classList.add('cbe-modal-open');

            const grid = overlay.querySelector('[data-media-grid]');
            const search = overlay.querySelector('[data-media-search]');
            const confirm = overlay.querySelector('[data-media-confirm]');
            const status = overlay.querySelector('[data-media-status]');
            let images = [];
            let selected = null;

            const close = () => {
                overlay.remove();
                document.body.classList.remove('cbe-modal-open');
                previousFocus?.focus?.();
            };

            const render = () => {
                const term = (search.value || '').trim().toLowerCase();
                const filtered = images.filter(image => {
                    if (!term) return true;
                    return (image.fileName || '').toLowerCase().includes(term)
                        || (image.alt || '').toLowerCase().includes(term);
                });

                grid.innerHTML = '';
                if (filtered.length === 0) {
                    grid.innerHTML = '<p class="cbe-media-empty">No hay imágenes disponibles.</p>';
                    return;
                }

                filtered.forEach(image => {
                    const card = element('button', 'cbe-media-card');
                    card.type = 'button';
                    card.setAttribute('aria-pressed', 'false');
                    card.innerHTML = `
                        <span class="cbe-media-thumb"><img src="${escapeHtml(image.thumbnailUrl || image.url || '')}" alt="${escapeHtml(image.alt || image.fileName || '')}" loading="lazy" /></span>
                        <span class="cbe-media-name">${escapeHtml(image.fileName || 'Imagen')}</span>
                    `;
                    card.addEventListener('click', () => {
                        selected = image;
                        confirm.disabled = false;
                        status.textContent = image.fileName || 'Imagen seleccionada';
                        grid.querySelectorAll('[aria-pressed="true"]').forEach(item => {
                            item.setAttribute('aria-pressed', 'false');
                            item.classList.remove('is-selected');
                        });
                        card.setAttribute('aria-pressed', 'true');
                        card.classList.add('is-selected');
                    });
                    grid.appendChild(card);
                });
            };

            overlay.querySelectorAll('[data-media-close], [data-media-cancel]')
                .forEach(button => button.addEventListener('click', close));
            overlay.addEventListener('click', event => {
                if (event.target === overlay) close();
            });
            overlay.addEventListener('keydown', event => {
                if (event.key === 'Escape') close();
            });
            search.addEventListener('input', render);
            search.focus();
            confirm.addEventListener('click', () => {
                if (!selected) return;
                callback(selected);
                close();
            });

            try {
                const response = await fetch(
                    `/admin/articulos/imagenes-disponibles?currentArticleId=${encodeURIComponent(this.articleId)}`);
                const data = await response.json();
                images = Array.isArray(data) ? data : [];
            } catch (error) {
                console.warn('No se pudo cargar la biblioteca de imágenes', error);
                status.textContent = 'No se pudo cargar la biblioteca.';
            }

            render();
        }
    }

    document.addEventListener('DOMContentLoaded', () => {
        document.querySelectorAll('[data-content-blocks-editor]').forEach(root => {
            const inputId = root.dataset.inputId || 'BlocksJsonInput';
            const input = document.getElementById(inputId);
            if (!input) return;

            const editor = new ContentBlocksEditor(root, input);
            editor.init();
            window.contentBlocksEditor = editor;
        });
    });
})();
