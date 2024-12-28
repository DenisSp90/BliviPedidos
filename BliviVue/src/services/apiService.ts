import type IProdutos from '@/interfaces/IProdutos';

export async function getProdutos() {
    const resposta = await fetch('http://localhost:5281/api/StoreApi/produtoList');
    const produtos : IProdutos[] = await resposta.json();

    return produtos;
}