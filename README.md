# Ocorrências — Excel, Python e AWS Glue

## Visão geral

Este projeto transforma uma planilha Excel habilitada para macros em uma interface simples para executar uma consulta Python, separar os dados conforme regras de negócio e carregar cada grupo na aba correta.

No protótipo atual, o Python lê `exemplo/dados_exemplo.csv`. Na versão corporativa, a função de consulta será substituída pelo código que cria uma AWS Glue Interactive Session e retorna o DataFrame analítico.

O analista recebe somente `Ocorrencias.exe`. O executável contém a planilha, o código Python e os arquivos necessários. Na primeira abertura ele pode preparar o ambiente do usuário; nas seguintes, abre diretamente a planilha instalada em `%LOCALAPPDATA%\Ocorrencias`.

## Estado atual

Implementado:

- botão do Excel executando Python;
- importação dinâmica de DataFrames com colunas variáveis;
- roteamento por `tipo_atividade`;
- arquivos de resultado com nomes derivados das abas;
- manifesto que informa ao VBA quais arquivos carregar e em quais abas;
- limpeza segura dos resultados anteriores;
- mensagem somente em caso de erro e registro da última atualização;
- empacotamento em um único EXE;
- preparação opcional de arquivos, variáveis, ambiente virtual e bibliotecas;
- descoberta automática do Python 3.12;
- configuração exclusivamente no escopo do usuário.

Pendente para a versão corporativa:

- substituir o CSV mockado pela consulta real do AWS Glue;
- incorporar ao Python a verificação de sessão AWS SSO e o login quando necessário;
- preencher proxies, certificado, perfil AWS e `pip.ini` reais;
- fixar as versões homologadas das bibliotecas;
- assinar digitalmente o executável e a macro conforme a política da empresa.

## Fluxo resumido

```text
Analista abre Ocorrencias.exe
        |
        v
Pacote é instalado/atualizado em %LOCALAPPDATA%\Ocorrencias
        |
        v
Preparação inicial do ambiente, se habilitada e ainda não concluída
        |
        v
consulta.xlsm é aberta
        |
        v
Botão "Atualizar dados" executa python\consulta.py
        |
        v
Python consulta, valida, roteia e cria CSVs + manifesto.csv
        |
        v
VBA lê o manifesto, atualiza as abas e registra data/hora
```

## Pré-requisitos corporativos

Responsabilidade do usuário ou da área de suporte:

- Windows com Microsoft Excel e macros autorizadas;
- Python **3.12**, instalado pela Central de Software;
- AWS CLI **2.24**, instalada pela Central de Software;
- acesso liberado à conta AWS e ao perfil utilizado pela área;
- conectividade, proxy e certificado corporativo válidos.

O pacote não instala o Python base, a AWS CLI nem concede permissões na AWS.

## Estrutura do repositório

| Caminho | Responsabilidade |
|---|---|
| `consulta.xlsm` | Interface utilizada pelo analista e macro de importação. |
| `python/consulta.py` | Consulta, validação, roteamento e geração dos resultados. |
| `vba/ModuloConsulta.bas` | Fonte de manutenção do módulo VBA. |
| `exemplo/dados_exemplo.csv` | Fonte mockada para desenvolvimento local. |
| `configuracao/ambiente.json` | Regras opcionais de preparação do computador. |
| `configuracao/bibliotecas/requirements.txt` | Dependências Python do pacote. |
| `build/Program.cs` | Código-fonte do lançador EXE. |
| `build/ocorrencias.ico` | Ícone do executável. |
| `exe/gerar-exe.ps1` | Script de compilação e empacotamento. |
| `dist/Ocorrencias.exe` | Artefato distribuível; é sobrescrito a cada compilação. |
| `saida/` | Resultados locais de desenvolvimento; não entra no EXE. |

## Documentação

- [Arquitetura e decisões](docs/ARQUITETURA.md)
- [Utilização pelo analista](docs/USO_DO_ANALISTA.md)
- [AWS SSO e Glue Interactive Sessions](docs/AWS_E_GLUE.md)
- [Manutenção e publicação](docs/MANUTENCAO.md)
- [Solução de problemas](docs/SOLUCAO_DE_PROBLEMAS.md)
- [Configuração do ambiente corporativo](configuracao/LEIA-ME.md)
- [Como gerar o executável](exe/como_gerar_exe.md)

## Segurança

Não inclua senhas, tokens, access keys, secret keys ou chaves privadas no repositório ou no executável. O desenho pressupõe autenticação federada pelo AWS IAM Identity Center, arquivos de configuração sem credenciais permanentes e variáveis criadas somente para o usuário atual.

