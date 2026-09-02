document.addEventListener('DOMContentLoaded', () => {
    document.addEventListener('click', async (event) => {
        const botao = event.target.closest('.adicionar-produto');
        if (!botao) return;

        event.preventDefault();
        await adicionarProduto(botao);
    });

    document.addEventListener('focusout', (event) => {
        if (event.target.matches('.update-quantidade')) atualizarQuantidade(event.target);
    });
});

async function adicionarProduto(botao) {
    const produtoId = botao.dataset.produtoId;
    if (!produtoId || botao.disabled) return;

    botao.disabled = true;
    try {
        const response = await fetch(`/Store/Carrinho/${encodeURIComponent(produtoId)}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ produto: produtoId })
        });
        if (!response.ok) throw new Error(await obterMensagemErro(response));

        const resultado = await response.json();
        renderizarItens(resultado.listaItens);
        document.getElementById('quantidadeItens').textContent = resultado.listaItens.length;
        document.getElementById('numeroPedido').textContent = resultado.listaItens[0]?.pedido?.id ?? '';
        document.getElementById('total').textContent = resultado.carrinhoViewModel.total.toFixed(2);

        await Swal.fire({ icon: 'success', title: 'Produto adicionado ao pedido com sucesso' });
    } catch (error) {
        await Swal.fire({
            icon: 'error',
            title: 'Oops...',
            text: error.message || 'Ocorreu um erro ao adicionar o produto ao carrinho. Por favor, tente novamente mais tarde.'
        });
    } finally {
        botao.disabled = false;
    }
}

function renderizarItens(itens) {
    const tbody = document.querySelector('#tblPedidos tbody');
    const fragmento = document.createDocumentFragment();

    itens.forEach((item) => {
        const linha = document.createElement('tr');
        linha.dataset.itemId = item.id;
        linha.className = 'table-primary';

        const produto = document.createElement('td');
        produto.textContent = item.produto.nome;

        const quantidade = document.createElement('td');
        const input = document.createElement('input');
        input.type = 'text';
        input.value = item.quantidade;
        input.inputMode = 'numeric';
        input.pattern = '\\d*';
        input.className = 'form-control text-center col-md-4 update-quantidade';
        input.style.width = '4em';
        input.addEventListener('input', () => {
            input.value = input.value.replace(/[^0-9]/g, '');
        });
        quantidade.appendChild(input);

        const subtotal = document.createElement('td');
        subtotal.className = 'subtotal';
        subtotal.textContent = Number(item.subtotal).toFixed(2);

        linha.append(produto, quantidade, subtotal);
        fragmento.appendChild(linha);
    });

    tbody.replaceChildren(fragmento);
}

async function atualizarQuantidade(input) {
    const linha = input.closest('[data-item-id]');
    if (!linha) return;

    const data = { Id: linha.dataset.itemId, Quantidade: input.value };
    try {
        const response = await fetch('/Store/UpdateQuantidade', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
        });
        if (!response.ok) throw new Error(await obterMensagemErro(response));

        const resultado = await response.json();
        const item = resultado.itemPedido;
        const carrinho = resultado.carrinhoViewModel;
        const linhaAtual = document.querySelector(`[data-item-id="${CSS.escape(String(item.id))}"]`);

        if (item.quantidade === 0) {
            linhaAtual?.remove();
        } else if (linhaAtual) {
            linhaAtual.querySelector('input').value = item.quantidade;
            linhaAtual.querySelector('.subtotal').textContent = item.subtotal.toFixed(2);
        }

        document.querySelectorAll('[numero-itens]').forEach((elemento) => {
            elemento.textContent = `Total: ${carrinho.itens.length} itens`;
        });
        document.getElementById('quantidadeItens').textContent = carrinho.itens.length;
        document.getElementById('total').textContent = carrinho.total.toFixed(2);
    } catch (error) {
        console.error(error);
    }
}

async function obterMensagemErro(response) {
    const texto = await response.text();
    if (!texto) return 'Não foi possível concluir a operação.';

    try {
        const json = JSON.parse(texto);
        return typeof json === 'string' ? json : json.message || json.title || texto;
    } catch {
        return texto;
    }
}
