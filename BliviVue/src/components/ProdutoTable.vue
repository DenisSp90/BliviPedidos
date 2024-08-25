<template>
    <div class="produto-tabela">
        <h2>Lista de Produtos</h2>
        <table v-if="produtos.length > 0">
            <thead>
                <tr>
                    <th>Código</th>
                    <th>Nome</th>
                    <th>Preço de Venda</th>
                    <th>Preço Pago</th>
                    <th>Quantidade</th>
                    <th>Tamanho</th>
                    <th>Foto</th>
                </tr>
            </thead>
            <tbody>
                <tr v-for="produto in produtos" :key="produto.id">
                    <td>{{ produto.codigo }}</td>
                    <td>{{ produto.nome }}</td>
                    <td>R$ {{ produto.precoVenda.toFixed(2) }}</td>
                    <td>R$ {{ produto.precoPago.toFixed(2) }}</td>
                    <td>{{ produto.quantidade }}</td>
                    <td>{{ produto.tamanho }}</td>
                    <td>
                        <img :src="`${baseURL}${produto.foto}`" alt="Foto do produto" class="produto-foto" />
                    </td>
                </tr>
            </tbody>
        </table>
        <p v-else>Nenhum produto disponível.</p>
    </div>
</template>

<script>
import apiService from '@/services/apiService';

export default {
    data() {
        return {
            produtos: [],
            baseURL: 'https://localhost:7277/', 
        };
    },
    created() {
        this.fetchProdutoList();
    },
    methods: {
        async fetchProdutoList() {
            try {
                const response = await apiService.getProdutoList();
                this.produtos = response.data; // Armazena os produtos na data do componente
            } catch (error) {
                console.error('Erro ao buscar lista de produtos:', error);
            }
        },
    },
};
</script>

<style scoped>
.produto-tabela {
    border: 1px solid #ddd;
    padding: 20px;
    border-radius: 8px;
    max-width: 1200px;
    margin: 20px auto;
    background-color: #f9f9f9;
}

table {
    width: 100%;
    border-collapse: collapse;
}

thead {
    background-color: #f4f4f4;
}

th,
td {
    padding: 10px;
    text-align: left;
    border: 1px solid #ddd;
}

tbody tr:nth-child(odd) {
    background-color: #f9f9f9;
}

tbody tr:nth-child(even) {
    background-color: #ffffff;
}

.produto-foto {
    max-width: 100px;
    border-radius: 8px;
    box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
}
</style>