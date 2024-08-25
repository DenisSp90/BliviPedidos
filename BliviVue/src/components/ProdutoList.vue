<template>
    <div>
      <h1>Lista de Produtos</h1>
      <ul>
        <li v-for="produto in produtos" :key="produto.id">
          {{ produto.nome }} - {{ produto.preco }}
        </li>
      </ul>
    </div>
  </template>

<script>
import apiService from '@/services/apiService';

export default {
  data() {
    return {
      produtos: []
    };
  },
  created() {
    this.fetchProdutoList();
  },
  methods: {
    async fetchProdutoList() {
      try {
        const response = await apiService.getProdutoList();
        this.produtos = response.data;
      } catch (error) {
        console.error('Erro ao buscar lista de produtos:', error);
      }
    }
  }
};
</script>

<style scoped>
/* Estilos opcionais para o componente */
h1 {
  color: #333;
}

ul {
  list-style-type: none;
  padding: 0;
}

li {
  margin: 8px 0;
}
</style>