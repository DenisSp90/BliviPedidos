import axios from 'axios';

const apiClient = axios.create({
    baseURL: 'http://localhost:5281/api/StoreApi/', 
    headers: {
      'Content-Type': 'application/json'
    }
  });

  export default {
    getProdutoList() {
      return apiClient.get('/produtoList');
    }
  };