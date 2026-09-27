# AWS SSO e Glue Interactive Sessions

## Estado da integração

O protótipo atual lê `exemplo/dados_exemplo.csv`. Este documento define o comportamento esperado quando `executar_consulta()` for substituída pela consulta corporativa já desenvolvida com AWS Glue Interactive Sessions.

## Perfil AWS

O perfil previsto é `sosdados`. No ambiente final, o arquivo corporativo de configuração deve ser instalado no local validado pela área. O padrão oficial da AWS no Windows é `%USERPROFILE%\.aws\config`; este protótipo está preparado para um destino configurável e atualmente usa `%APPDATA%\.aws\config`. Confirme o caminho efetivo no trabalho antes de habilitar a cópia.

Não inclua access keys, secret keys ou tokens no executável. Para AWS IAM Identity Center, o arquivo `config` descreve perfil e sessão; o login gera credenciais temporárias em cache.

## Fluxo de autenticação recomendado

Antes de criar a Interactive Session, o Python corporativo deve:

1. executar uma verificação silenciosa:

   ```powershell
   aws sts get-caller-identity --profile sosdados --no-cli-pager
   ```

2. se o comando retornar código zero, continuar sem abrir login;
3. se falhar por sessão ausente ou expirada, executar:

   ```powershell
   aws sso login --profile sosdados --no-cli-pager
   ```

4. aguardar a autenticação no navegador;
5. executar novamente `get-caller-identity`;
6. somente iniciar o Glue se a segunda verificação tiver sucesso.

`sts get-caller-identity` retorna a identidade associada às credenciais atuais e é adequado para testar se o perfil consegue autenticar. `--no-cli-pager` impede que a AWS CLI abra o paginador de saída (`more` no Windows), o que poderia bloquear uma execução invisível iniciada pelo Excel.

Esse fluxo ainda não foi incorporado ao `python/consulta.py` mockado.

## Glue Interactive Sessions

As Interactive Sessions permitem desenvolver e executar código Apache Spark do AWS Glue a partir de um ambiente local. O pacote `aws-glue-sessions` fornece os kernels e o comando `install-glue-kernels` registra os kernels `pyspark` e `spark` no Jupyter.

O ambiente virtual do projeto instala atualmente:

- `pandas`;
- `boto3`;
- `jupyter`;
- `aws-glue-sessions`.

No ambiente corporativo, fixe versões compatíveis e homologadas no `requirements.txt`.

## Integração com o projeto

A implementação real deve preservar o contrato atual:

```python
def executar_consulta() -> pd.DataFrame:
    # autenticar, criar/reutilizar a sessão Glue,
    # executar o analítico e converter o resultado
    return dataframe
```

Todo o restante — validação, roteamento, geração de CSVs, manifesto e importação no Excel — pode permanecer igual.

Cuidados para a implementação:

- encerrar ou reutilizar sessões conforme a política da área, evitando custo desnecessário;
- configurar região, role e versão do Glue explicitamente;
- propagar somente mensagens apropriadas ao usuário;
- registrar detalhes técnicos em log, sem credenciais;
- tratar timeout e falha de criação da sessão;
- garantir que o resultado final seja um DataFrame pandas antes do roteamento.

## Referências oficiais

- [Configurar autenticação do IAM Identity Center na AWS CLI](https://docs.aws.amazon.com/cli/latest/userguide/cli-configure-sso.html)
- [Arquivos de configuração e credenciais da AWS CLI](https://docs.aws.amazon.com/cli/latest/userguide/cli-configure-files.html)
- [Referência de `aws sts get-caller-identity`](https://docs.aws.amazon.com/cli/latest/reference/sts/get-caller-identity.html)
- [Paginação e `--no-cli-pager` na AWS CLI](https://docs.aws.amazon.com/cli/latest/userguide/cli-usage-pagination.html)
- [AWS Glue Interactive Sessions](https://docs.aws.amazon.com/glue/latest/dg/interactive-sessions.html)
- [Preços do AWS Glue](https://aws.amazon.com/glue/pricing/)

