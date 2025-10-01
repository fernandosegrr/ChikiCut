// Íconos para servicios y productos (puedes personalizar más)
window.PV_EMOJIS = {
    servicios: [
        '✂️', '👧', '✨', '👑', '🧚‍♀️',
        '🌟', '💅', '🦶', '😊', '🎨'
    ],
    productos: [
        '💧', '🧴', '🖌️', '💨', '💦',
        '😎', '⭐', '✂️', '🖌️', '🌪️',
        '🧢', '🎁', '🦄', '🧼', '🧴'
    ]
};

// Datos mock (deben coincidir con los del PageModel)
window.PV_CLIENTES = [
    "Juan Pérez", "María López", "Carlos Sánchez", "Ana Torres", "Luis Ramírez", "Sofía Mendoza", "Pedro Castillo", "Lucía Herrera", "Miguel Díaz", "Valeria Gómez",
    "Andrea Morales", "Jorge Ruiz", "Fernanda Castro", "Emilia Vargas", "Mateo Silva", "Camila Ortega", "Diego Ríos", "Paula Aguirre", "Santiago León", "Cliente de mostrador"
];
window.PV_SERVICIOS = [
    { nombre: "Corte niño", precio: 150 },
    { nombre: "Corte niña", precio: 170 },
    { nombre: "Peinado especial", precio: 200 },
    { nombre: "Trenza francesa", precio: 120 },
    { nombre: "Trenza boxeadora", precio: 130 },
    { nombre: "Peinado con glitter", precio: 180 },
    { nombre: "Manicure infantil", precio: 100 },
    { nombre: "Pedicure infantil", precio: 110 },
    { nombre: "Mascarilla facial kids", precio: 90 },
    { nombre: "Maquillaje fantasía", precio: 160 }
];
window.PV_PRODUCTOS = [
    { nombre: "Gel para cabello", precio: 80 },
    { nombre: "Shampoo infantil", precio: 120 },
    { nombre: "Peine de colores", precio: 50 },
    { nombre: "Spray desenredante", precio: 90 },
    { nombre: "Cera modeladora", precio: 110 },
    { nombre: "Diadema unicornio", precio: 60 },
    { nombre: "Moño fantasía", precio: 45 },
    { nombre: "Tijeras infantiles", precio: 150 },
    { nombre: "Set brochas", precio: 130 },
    { nombre: "Toalla estampada", precio: 70 },
    { nombre: "Cepillo anti-tirones", precio: 95 },
    { nombre: "Mascarilla capilar", precio: 140 },
    { nombre: "Brillantina", precio: 55 },
    { nombre: "Kit peinado", precio: 160 },
    { nombre: "Gorro de baño", precio: 35 }
];
window.PV_METODOS = [
    { nombre: 'Efectivo', icon: 'cash' },
    { nombre: 'Tarjeta', icon: 'credit-card' },
    { nombre: 'Transferencia', icon: 'bank' }
];

// Estado de la venta
window.PV_VENTA = {
    cliente: '',
    items: [], // {tipo, nombre, precio, cantidad}
    pagos: []  // {metodo, monto, referencia}
};

// Utilidades para actualizar la UI y lógica
window.PV_updateCliente = function(val) {
    PV_VENTA.cliente = val;
    document.getElementById('pv-cliente-input').value = val;
};

window.PV_buscar = function(tipo) {
    let val = document.getElementById('pv-buscar-' + tipo).value.toLowerCase();
    let arr = tipo === 'servicio' ? PV_SERVICIOS : PV_PRODUCTOS;
    let emojis = tipo === 'servicio' ? PV_EMOJIS.servicios : PV_EMOJIS.productos;
    let grid = document.getElementById('pv-grid-' + tipo);
    grid.innerHTML = '';
    arr.forEach((item, idx) => {
        if (item.nombre.toLowerCase().includes(val)) {
            let emoji = emojis[idx % emojis.length];
            grid.innerHTML += `<button class='pv-btn' onclick='PV_addItem("${tipo}",${idx})'>
                <span class='pv-emoji'>${emoji}</span>
                <span class='pv-label'>${item.nombre.replace(new RegExp(val, 'gi'), match => `<mark>${match}</mark>`)}</span>
                <span class='pv-price'>$${item.precio}</span>
            </button>`;
        }
    });
};

window.PV_addItem = function(tipo, idx) {
    let arr = tipo === 'servicio' ? PV_SERVICIOS : PV_PRODUCTOS;
    let nombre = arr[idx].nombre;
    let precio = arr[idx].precio;
    let found = PV_VENTA.items.find(x => x.tipo === tipo && x.nombre === nombre);
    if (found) found.cantidad++;
    else PV_VENTA.items.push({ tipo, nombre, precio, cantidad: 1, idx });
    PV_renderVenta();
    PV_updateTotalSticky();
};

window.PV_updateTotalSticky = function() {
    let total = PV_VENTA.items.reduce((a, b) => a + b.precio * b.cantidad, 0);
    let el = document.getElementById('pv-total');
    if (el) el.textContent = '$' + total.toFixed(2);
};

window.PV_renderVenta = function() {
    let tbody = document.getElementById('pv-visor-tbody');
    tbody.innerHTML = '';
    let total = 0;
    PV_VENTA.items.forEach((item, idx) => {
        let sub = item.precio * item.cantidad;
        total += sub;
        let emoji = item.tipo === 'servicio' ? PV_EMOJIS.servicios[item.idx % PV_EMOJIS.servicios.length] : PV_EMOJIS.productos[item.idx % PV_EMOJIS.productos.length];
        tbody.innerHTML += `<tr>
            <td><span class="pv-emoji">${emoji}</span> ${item.nombre}</td>
            <td><input type='number' min='1' value='${item.cantidad}' class='form-control form-control-sm' style='width:70px' onchange='PV_updateCantidad(${idx}, this.value)' /></td>
            <td>$${item.precio}</td>
            <td>$${sub}</td>
            <td><button class='btn btn-outline-danger btn-sm' onclick='PV_removeItem(${idx})'><i class='bi bi-x'></i></button></td>
        </tr>`;
    });
    PV_updateTotalSticky();
    PV_renderPagos();
};

window.PV_renderPagos = function() {
    let pagosDiv = document.getElementById('pv-pagos-list');
    pagosDiv.innerHTML = '';
    let pagado = 0;
    PV_VENTA.pagos.forEach((p, idx) => {
        pagado += p.monto;
        pagosDiv.innerHTML += `<div class='d-flex align-items-center mb-1'>
            <span class='badge bg-secondary me-2'><i class='bi bi-${PV_METODOS.find(m=>m.nombre===p.metodo).icon}'></i> ${p.metodo}</span>
            <span class='me-2'>$${p.monto.toFixed(2)}</span>
            ${p.referencia ? `<span class='text-muted small me-2'>${p.referencia}</span>` : ''}
            <button class='btn btn-outline-danger btn-sm' onclick='PV_removePago(${idx})'><i class='bi bi-x'></i></button>
        </div>`;
    });
    let total = PV_VENTA.items.reduce((a, b) => a + b.precio * b.cantidad, 0);
    let restante = total - pagado;
    document.getElementById('pv-restante').textContent = '$' + (restante > 0 ? restante.toFixed(2) : '0.00');
};

window.PV_clienteAutocomplete = function() {
    let input = document.getElementById('pv-cliente-input');
    let datalist = document.getElementById('pv-clientes-list');
    let sugerencia = document.getElementById('pv-cliente-sugerencia');
    let val = input.value.toLowerCase();
    datalist.innerHTML = '';
    let found = false;
    PV_CLIENTES.forEach(c => {
        if (c.toLowerCase().includes(val)) {
            let opt = document.createElement('option');
            opt.value = c;
            datalist.appendChild(opt);
            if (c.toLowerCase() === val) found = true;
        }
    });
    if (val.length > 0 && !found) {
        sugerencia.innerHTML = "Cliente no encontrado. <span class='text-primary' style='cursor:pointer'>Agregar nuevo</span>";
    } else {
        sugerencia.innerHTML = '';
    }
    PV_updateCliente(input.value);
};

document.addEventListener('DOMContentLoaded', function() {
    PV_buscar('servicio');
    PV_buscar('producto');
    PV_renderVenta();
    // Autocomplete para clientes
    let input = document.getElementById('pv-cliente-input');
    let datalist = document.getElementById('pv-clientes-list');
    let sugerencia = document.getElementById('pv-cliente-sugerencia');
    input.addEventListener('input', function() {
        let val = input.value.toLowerCase();
        datalist.innerHTML = '';
        let found = false;
        PV_CLIENTES.forEach(c => {
            if (c.toLowerCase().includes(val)) {
                let opt = document.createElement('option');
                opt.value = c;
                datalist.appendChild(opt);
                if (c.toLowerCase() === val) found = true;
            }
        });
        if (val.length > 0 && !found) {
            sugerencia.innerHTML = "Cliente no encontrado. <span class='text-primary' style='cursor:pointer'>Agregar nuevo</span>";
        } else {
            sugerencia.innerHTML = '';
        }
        PV_updateCliente(input.value);
    });
    // Trigger inicial para mostrar sugerencias si hay valor
    input.dispatchEvent(new Event('input'));
});
