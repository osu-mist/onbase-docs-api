const fakeOsuId = '999999999';
const fakeBaseUrl = '/v2';
const fakeDocument = { typeId: 'fake id' };

const docType1 = {
    id: '24680',
    name: 'TestDocType',
    typeId: '924680',
};
const fakeData = {
    data: {
        access_token: 'fake token',
        keywordGuid: 'fake guid',
        keywordCollection: 'fake keyword collection',
        id: 'fake id',
        items: [docType1],
    },
};
  
const keyword1 = {
    name: 'Keyword1',
    typeId: '924680',
    keywordTypeId: '924680',
};
const keyword2 = {
    name: 'Keyword2',
    typeId: '9123456',
    keywordTypeId: '9123456',
};
const keywordType1 = {
    id: '924680',
    name: 'KeywordType1',
};
const keywordType2 = {
    id: '9123456',
    name: 'KeywordType2',
};
const fakeKeywordsTypesData = {
    data: {
        access_token: 'fake token',
        keywordGuid: 'fake guid',
        keywordCollection: 'fake keyword collection',
        id: 'fake id',
        items: [keywordType1, keywordType2],
    },
};

export {
    fakeOsuId,
    fakeBaseUrl,
    fakeDocument,
    docType1,
    fakeData,
    keyword1,
    keyword2,
    keywordType1,
    keywordType2,
    fakeKeywordsTypesData,
};
