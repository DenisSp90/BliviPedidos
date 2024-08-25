import axios from 'axios';

const apiClient = axios.create({
    baseURL: 'https://localhost:7277/api/StoreApi/', // URL base do seu API ASP.NET Core
    headers: {
      'Content-Type': 'application/json'
    }
  });

  export default {
    getProdutoList() {
      return apiClient.get('/produtoList');
    }
  };