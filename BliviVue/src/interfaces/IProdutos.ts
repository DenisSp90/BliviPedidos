export default interface IProduct {
    id: number;
    codigo?: string;
    nome: string;
    precoVenda: number;
    precoPago: number;
    quantidade: number;
    tamanho?: string;
}