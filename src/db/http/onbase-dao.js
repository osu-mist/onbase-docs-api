import axios from 'axios';
import config from 'config';
import FormData from 'form-data';
import _ from 'lodash';
import setCookie from 'set-cookie-parser';

import { logger } from 'utils/logger';

const {
  baseUri,
  apiServer,
  idpServer,
  tenant,
  clientId,
  clientSecret,
  onbaseProfiles,
  documentIndexKeyTypeId,
} = config.get('dataSources.http');

const onbaseIdpUrl = `${baseUri}/app/${idpServer}`;
const onbaseIndexingModifiersUrl = `${baseUri}/app/${apiServer}/onbase/core/indexing-modifiers`;
const onbaseDocumentsUrl = `${baseUri}/app/${apiServer}/onbase/core/documents`;
const onbaseDocumentTypesUrl = `${baseUri}/app/${apiServer}/onbase/core/document-types`;
const onbaseKeywordTypesUrl = `${baseUri}/app/${apiServer}/onbase/core/keyword-types`;

/**
 * Get FB_LB cookie token from response headers
 *
 * @param {string} res response object
 * @returns {string} FB_LB cookie value
 */
const getFbLbCookie = (res) => {
  const cookies = setCookie.parse(res);
  const fbLb = _.filter(cookies, { name: 'FB_LB' })[0];
  return fbLb.value;
};

/**
 * Get API access token from OnBase IDP server
 *
 * @param {string} onbaseProfile OnBase profile name
 * @returns {Promise} resolves if fetched access token or rejects otherwise
 */
const getAccessToken = async (onbaseProfile) => {
  try {
    const { username, password } = onbaseProfiles[onbaseProfile];

    const formData = new FormData();
    formData.append('grant_type', 'password');
    formData.append('scope', 'evolution');
    formData.append('tenant', tenant);
    formData.append('client_id', clientId);
    formData.append('client_secret', clientSecret);
    formData.append('username', username);
    formData.append('password', password);

    const reqConfig = {
      method: 'post',
      url: `${onbaseIdpUrl}/connect/token`,
      headers: formData.getHeaders(),
      data: formData,
      withCredentials: true,
    };

    const res = await axios(reqConfig);
    const { data: { access_token: accessToken } } = res;
    return [accessToken, getFbLbCookie(res)];
  } catch (err) {
    if (err.response && err.response.status !== 200) {
      logger.error(err.response.data.error);
      throw new Error(err.response.data.error_description);
    } else {
      logger.error(err);
      throw new Error(err);
    }
  }
};

/**
 * Prepares the staging area to start the upload. Returns a reference to the file being uploaded
 *
 * @param {string} token access token
 * @param {string} fbLb FB_LB cookie value
 * @param {string} fileExtension file extension
 * @param {number} fileSize file size
 * @returns {Promise} resolves if staging area initialized or rejects otherwise
 */
const initiateStagingArea = async (token, fbLb, fileExtension, fileSize) => {
  try {
    const reqConfig = {
      method: 'post',
      url: `${onbaseDocumentsUrl}/uploads`,
      headers: {
        Authorization: `Bearer ${token}`,
        Cookie: `FB_LB=${fbLb}`,
      },
      data: { fileExtension, fileSize },
      withCredentials: true,
    };

    const res = await axios(reqConfig);

    return [res.data, getFbLbCookie(res)];
  } catch (err) {
    if (err.response && err.response.status !== 201) {
      logger.error(err.response.data.errors);
      throw new Error(err.response.data.detail);
    } else {
      logger.error(err);
      throw new Error(err);
    }
  }
};

/**
 * Prepares the staging area to start the upload. Returns a reference to the file being uploaded
 *
 * @param {string} token access token
 * @param {string} fbLb FB_LB cookie value
 * @param {string} uploadId the unique reference to the file being uploaded
 * @param {number} filePart part number of the file to upload
 * @param {string} mimeType media type
 * @param {object} fileBuffer binary content for file upload
 * @returns {Promise} resolves if file uploaded or rejects otherwise
 */
const uploadFile = async (token, fbLb, uploadId, filePart, mimeType, fileBuffer) => {
  try {
    const reqConfig = {
      method: 'put',
      url: `${onbaseDocumentsUrl}/uploads/${uploadId}`,
      headers: {
        Authorization: `Bearer ${token}`,
        'Content-Type': mimeType,
        Cookie: `FB_LB=${fbLb}`,
      },
      params: { filePart },
      data: fileBuffer,
      maxContentLength: Infinity,
      maxBodyLength: Infinity,
      withCredentials: true,
    };

    const res = await axios(reqConfig);
    return [getFbLbCookie(res)];
  } catch (err) {
    if (err.response && err.response.status === 413) {
      logger.error(err.response.data);
      return new Error(err.response.data);
    } if (err.response && err.response.status !== 204) {
      logger.error(err.response.data.errors);
      throw new Error(err.response.data.detail);
    } else {
      logger.error(err);
      throw new Error(err);
    }
  }
};

/**
 * Get default keywords GUID string to ensure integrity of restricted keyword values
 *
 * @param {string} token access token
 * @param {string} fbLb FB_LB cookie value
 * @param {string} documentTypeId the unique identifier of a document type
 * @returns {Promise} resolves if keywords GUID fetched or rejects otherwise
 */
const getDefaultKeywordsGuid = async (token, fbLb, documentTypeId) => {
  try {
    const reqConfig = {
      method: 'get',
      url: `${onbaseDocumentTypesUrl}/${documentTypeId}/default-keywords`,
      headers: {
        Authorization: `Bearer ${token}`,
        Cookie: `FB_LB=${fbLb}`,
      },
      withCredentials: true,
    };

    const res = await axios(reqConfig);
    const { keywordGuid } = res.data;
    return [keywordGuid, getFbLbCookie(res)];
  } catch (err) {
    if (err.response && err.response.status === 404) {
      const error = err.response.data.detail || err.response.data.errors;
      logger.error(error);
      return new Error(error);
    } if (err.response && err.response.status !== 200) {
      logger.error(err.response.data.errors);
      throw new Error(err.response.data.detail);
    }
    logger.error(err);
    throw new Error(err);
  }
};

/**
 * Finishes the document upload by archiving the document into the given document type
 *
 * @param {string} token access token
 * @param {string} fbLb FB_LB cookie value
 * @param {string} documentTypeId document type ID
 * @param {string} defaultKeywordsGuid default keywords GUID
 * @param {string} indexKey index key
 * @returns {Promise} resolves if document archived successfully or rejects otherwise
 */
const postIndexingModifiers = async (
  token,
  fbLb,
  documentTypeId,
  defaultKeywordsGuid,
  indexKey,
) => {
  try {
    const reqConfig = {
      method: 'post',
      url: `${onbaseIndexingModifiersUrl}`,
      headers: {
        Authorization: `Bearer ${token}`,
        Cookie: `FB_LB=${fbLb}`,
      },
      data: {
        objectType: 'ArchivalAutoFillExpansion',
        documentTypeId,
        autoFillKeywordSetPrimaryKeyword: {
          typeId: documentIndexKeyTypeId,
          value: indexKey,
        },
        keywordCollection: {
          keywordGuid: defaultKeywordsGuid,
          items: [
            {
              keywords: [
                {
                  typeId: documentIndexKeyTypeId,
                  values: [
                    {
                      value: indexKey,
                    },
                  ],
                },
              ],
            },
          ],
        },
      },
      withCredentials: true,
    };

    const res = await axios(reqConfig);
    const { keywordCollection } = res.data;

    return [keywordCollection, getFbLbCookie(res)];
  } catch (err) {
    if (err.response && err.response.status === 400) {
      const error = err.response.data.detail || err.response.data.errors;
      logger.error(error);
      return new Error(error);
    }
    if (err.response && err.response.status !== 201) {
      logger.error(err.response.data.errors);
      throw new Error(err.response.data.detail);
    } else {
      logger.error(err);
      throw new Error(err);
    }
  }
};

/**
 * Finishes the document upload by archiving the document into the given document type
 *
 * @param {string} token access token
 * @param {string} fbLb FB_LB cookie value
 * @param {string} documentTypeId the unique identifier of a document type
 * @param {string} uploadId file uploaded ID
 * @param {string} keywordCollection keyword collection
 * @returns {Promise} resolves if document archived successfully or rejects otherwise
 */
const archiveDocument = async (
  token,
  fbLb,
  documentTypeId,
  uploadId,
  keywordCollection,
) => {
  try {
    const reqConfig = {
      method: 'post',
      url: `${onbaseDocumentsUrl}`,
      headers: {
        Authorization: `Bearer ${token}`,
        Cookie: `FB_LB=${fbLb}`,
      },
      data: {
        documentTypeId,
        uploads: [{ id: uploadId }],
        keywordCollection,
      },
      withCredentials: true,
    };

    const res = await axios(reqConfig);
    const {
      data: { id: documentId },
    } = res;
    return [documentId, getFbLbCookie(res)];
  } catch (err) {
    if (err.response && err.response.status !== 201) {
      logger.error(err.response.data.errors);
      throw new Error(err.response.data.detail);
    } else {
      logger.error(err);
      throw new Error(err);
    }
  }
};

/**
 * Get document metadata
 *
 * @param {string} token access token
 * @param {string} fbLb FB_LB cookie value
 * @param {string} documentId the unique identifier of a document
 * @returns {Promise} resolves if document meta data fetched successfully or rejects otherwise
 */
const getDocumentById = async (token, fbLb, documentId) => {
  try {
    const reqConfig = {
      method: 'get',
      url: `${onbaseDocumentsUrl}/${documentId}`,
      headers: {
        Authorization: `Bearer ${token}`,
        Cookie: `FB_LB=${fbLb}`,
      },
      withCredentials: true,
    };

    const { data } = await axios(reqConfig);
    return data;
  } catch (err) {
    if (err.response && err.response.status !== 200) {
      logger.error(err.response.data.errors);
      throw new Error(err.response.data.detail);
    } else {
      logger.error(err);
      throw new Error(err);
    }
  }
};

/**
 * Get document keywords
 *
 * @param {string} token access token
 * @param {string} fbLb FB_LB cookie value
 * @param {string} documentId the unique identifier of a document type
 * @returns {Promise} resolves if document keywords fetched or rejects otherwise
 */
const getDocumentKeywords = async (token, fbLb, documentId) => {
  try {
    const reqConfig = {
      method: 'get',
      url: `${onbaseDocumentsUrl}/${documentId}/keywords`,
      headers: {
        Authorization: `Bearer ${token}`,
        Cookie: `FB_LB=${fbLb}`,
      },
      withCredentials: true,
    };

    const res = await axios(reqConfig);
    return [res.data, getFbLbCookie(res)];
  } catch (err) {
    if (err.response && err.response.status === 404) {
      logger.error(err.response.data.errors);
      return new Error(err.response.data.detail);
    } if (err.response && err.response.status !== 200) {
      logger.error(err.response.data.errors);
      throw new Error(err.response.data.detail);
    } else {
      logger.error(err);
      throw new Error(err);
    }
  }
};

/**
 * Get document keywords
 *
 * @param {string} token access token
 * @param {string} fbLb FB_LB cookie value
 * @param {Object[]} keywords document keywords
 * @returns {Promise} resolves if document keywords fetched or rejects otherwise
 */
const getDocumentKeywordTypes = async (token, fbLb, keywords) => {
  try {
    const params = new URLSearchParams();
    _.forEach(keywords, ({ typeId }) => {
      params.append('id', typeId);
    });

    const reqConfig = {
      method: 'get',
      url: `${onbaseKeywordTypesUrl}`,
      headers: {
        Authorization: `Bearer ${token}`,
        Cookie: `FB_LB=${fbLb}`,
      },
      params,
      withCredentials: true,
    };

    const res = await axios(reqConfig);
    const keywordTypes = _.reduce(
      res.data.items,
      (result, keywordType) => {
        result[keywordType.id] = { name: keywordType.name };
        return result;
      },
      {},
    );

    _.forEach(keywords, (keyword) => {
      keyword.name = keywordTypes[keyword.typeId].name;
    });

    return [keywords, getFbLbCookie(res)];
  } catch (err) {
    if (err.response && err.response.status === 404) {
      logger.error(err.response.data.errors);
      return new Error(err.response.data.detail);
    }
    if (err.response && err.response.status !== 200) {
      logger.error(err.response.data.errors);
      throw new Error(err.response.data.detail);
    } else {
      logger.error(err);
      throw new Error(err);
    }
  }
};

/**
 * Get document metadata
 * @param {string} token access token
 * @param {string} fbLb FB_LB cookie value
 * @param {string} documentId the unique identifier of a document.
 * @param {object} currentKeywordCollection current keyword collection
 * @param {object} newKeywords new keywords object
 * @returns {Promise} resolves if document meta data fetched successfully or rejects otherwise
 */
const patchDocumentKeywords = async (
  token,
  fbLb,
  documentId,
  currentKeywordCollection,
  newKeywords,
) => {
  try {
    const newKeywordsMap = {};
    _.forEach(newKeywords, (newKeyword) => {
      newKeywordsMap[newKeyword.keywordTypeId] = _.map(newKeyword.values, (value) => ({ value }));
    });

    _.forEach(currentKeywordCollection.items[0].keywords, (keyword) => {
      if (_.has(newKeywordsMap, keyword.typeId)) {
        keyword.values = newKeywordsMap[keyword.typeId];
      }
    });

    const reqConfig = {
      method: 'put',
      url: `${onbaseDocumentsUrl}/${documentId}/keywords`,
      headers: {
        Authorization: `Bearer ${token}`,
        Cookie: `FB_LB=${fbLb}`,
      },
      data: currentKeywordCollection,
      withCredentials: true,
    };

    const res = await axios(reqConfig);
    return [res.data, getFbLbCookie(res)];
  } catch (err) {
    logger.error(err);
    if (err.response && err.response.status !== 200) {
      logger.error(err.response.data.errors);
      throw new Error(err.response.data.detail);
    } else {
      throw new Error(err);
    }
  }
};

/**
 * Get document content
 *
 * @param {string} token access token
 * @param {string} fbLb FB_LB cookie value
 * @param {string} documentId the unique identifier of a document type
 * @returns {Promise} resolves if document content fetched or rejects otherwise
 */
const getDocumentContent = async (token, fbLb, documentId) => {
  try {
    const revisionId = 'latest';
    const fileTypeId = 'default';

    const reqConfig = {
      method: 'get',
      url: `${onbaseDocumentsUrl}/${documentId}/revisions/${revisionId}/renditions/${fileTypeId}/content`,
      headers: {
        Authorization: `Bearer ${token}`,
        Cookie: `FB_LB=${fbLb}`,
      },
      responseType: 'arraybuffer',
      withCredentials: true,
    };

    const res = await axios(reqConfig);
    return [res, getFbLbCookie(res)];
  } catch (err) {
    logger.error(err);
    if (err.response && err.response.status === 404) {
      logger.error(err.response.data.errors);
      return new Error(err.response.data.detail);
    } if (err.response && (err.response.status !== 200 || err.response.status !== 206)) {
      logger.error(err.response.data.errors);
      throw new Error(err.response.data.detail);
    }
    throw new Error(err);
  }
};

/**
 * Get document type ID
 *
 * @param {string} token access token
 * @param {string} fbLb FB_LB cookie value
 * @param {Object} documentTypeName document type name
 * @returns {Promise} resolves if document type ID fetched or rejects otherwise
 */
const getDocumentTypeByName = async (token, fbLb, documentTypeName) => {
  try {
    const reqConfig = {
      method: 'get',
      url: `${onbaseDocumentTypesUrl}`,
      headers: {
        Authorization: `Bearer ${token}`,
        Cookie: `FB_LB=${fbLb}`,
      },
      params: { systemName: documentTypeName },
      withCredentials: true,
    };

    const res = await axios(reqConfig);

    if (res.data.items.length > 1) {
      const errMessage = 'More than one document types matched.';
      logger.error(errMessage);
      return new Error(errMessage);
    }

    if (res.data.items.length === 0) {
      const errMessage = 'Please provide a valid document type.';
      logger.error(errMessage);
      return new Error(errMessage);
    }

    return [res.data.items[0].id, getFbLbCookie(res)];
  } catch (err) {
    if (err.response && err.response.status !== 200) {
      logger.error(err.response.data.errors);
      throw new Error(err.response.data.detail);
    } else {
      logger.error(err);
      throw new Error(err);
    }
  }
};

/**
 * Get keyword type IDs
 *
 * @param {string} token access token
 * @param {string} fbLb FB_LB cookie value
 * @param {Object} query query parameters
 * @returns {Promise} resolves if keyword type IDs fetched or rejects otherwise
 */
const getKeywordTypesByNames = async (token, fbLb, query) => {
  try {
    const keywordTypes = _.reduce(
      _.range(query.keywordTypeNames.length),
      (result, i) => {
        result[query.keywordTypeNames[i]] = { value: query.keywordValues[i] };
        return result;
      },
      {},
    );

    const params = new URLSearchParams();
    _.forEach(query.keywordTypeNames, (keywordTypeName) => {
      params.append('systemName', keywordTypeName);
    });

    const reqConfig = {
      method: 'get',
      url: `${onbaseKeywordTypesUrl}`,
      headers: {
        Authorization: `Bearer ${token}`,
        Cookie: `FB_LB=${fbLb}`,
      },
      params,
      withCredentials: true,
    };

    const res = await axios(reqConfig);

    _.forEach(res.data.items, (keywordType) => {
      keywordTypes[keywordType.name].id = keywordType.id;
    });

    return [keywordTypes, getFbLbCookie(res)];
  } catch (err) {
    if (err.response && err.response.status !== 200) {
      logger.error(err.response.data.errors);
      throw new Error(err.response.data.detail);
    } else {
      logger.error(err);
      throw new Error(err);
    }
  }
};

/**
 * Generate query
 *
 * @param {string} token access token
 * @param {string} fbLb FB_LB cookie value
 * @param {string} documentTypeId document type ID
 * @param {Object} keywordTypes keyword types
 * @param {string} startDate document start date
 * @param {string} endDate document end date
 * @returns {Promise} resolves if document fetched or rejects otherwise
 */
const createQuery = async (token, fbLb, documentTypeId, keywordTypes, startDate, endDate) => {
  try {
    // Generate query keyword collection
    const queryKeywordCollection = _.reduce(
      keywordTypes,
      (result, keywordType) => {
        result.push({
          typeId: keywordType.id,
          value: keywordType.value,
          operator: 'Equal',
          relation: 'And',
        });
        return result;
      },
      [],
    );

    // Generate query document date range
    const documentDateRange = {};
    if (startDate) {
      documentDateRange.start = startDate;
    }
    if (endDate) {
      documentDateRange.end = endDate;
    }

    const reqConfig = {
      method: 'post',
      url: `${onbaseDocumentsUrl}/queries`,
      headers: {
        Authorization: `Bearer ${token}`,
        Cookie: `FB_LB=${fbLb}`,
      },
      data: {
        queryType: [
          {
            type: 'DocumentType',
            ids: [documentTypeId],
          },
        ],
        queryKeywordCollection,
        userDisplayColumns: [
          {
            displayColumnType: 'DocumentName',
          },
          {
            displayColumnType: 'DocumentDate',
          },
        ],
        documentDateRangeCollection: [documentDateRange],
        maxResults: 100,
      },
      withCredentials: true,
    };

    const res = await axios(reqConfig);

    return [res.data.id, getFbLbCookie(res)];
  } catch (err) {
    if (err.response && err.response.status !== 200) {
      const errorDetail = err.response.data.detail;
      if (
        errorDetail
        && (errorDetail.startsWith('An Error occurred while parsing a keyword value')
          || errorDetail.startsWith('Some of the provided input data is invalid')
        )
      ) {
        logger.warn(errorDetail);
        err.message = 'Unable to generate searching query. Please ensure providing valid inputs.';
        return new Error(err);
      }

      logger.error(err.response.data.errors);
      throw new Error(err.response.data.detail);
    } else {
      logger.error(err);
      throw new Error(err);
    }
  }
};

/**
 * Get query results
 *
 * @param {string} token access token
 * @param {string} fbLb FB_LB cookie value
 * @param {string} queryId query ID
 * @returns {Promise} resolves if document fetched or rejects otherwise
 */
const getQueryResults = async (token, fbLb, queryId) => {
  try {
    const reqConfig = {
      method: 'get',
      url: `${onbaseDocumentsUrl}/queries/${queryId}/results`,
      headers: {
        Authorization: `Bearer ${token}`,
        Cookie: `FB_LB=${fbLb}`,
      },
      withCredentials: true,
    };

    const res = await axios(reqConfig);

    return [res, getFbLbCookie(res)];
  } catch (err) {
    if (err.response && err.response.status !== 200) {
      logger.error(err.response.data.errors);
      throw new Error(err.response.data.detail);
    } else {
      logger.error(err);
      throw new Error(err);
    }
  }
};

/**
 * Get documents metadata
 *
 * @param {string} token access token
 * @param {string} fbLb FB_LB cookie value
 * @param {string[]} documentIds the unique identifiers of documents
 * @returns {Promise} resolves if documents meta data fetched successfully or rejects otherwise
 */
const getDocumentsByIds = async (token, fbLb, documentIds) => {
  try {
    const params = new URLSearchParams();
    _.forEach(documentIds, (documentId) => {
      params.append('id', documentId);
    });

    const reqConfig = {
      method: 'get',
      url: `${onbaseDocumentsUrl}`,
      headers: {
        Authorization: `Bearer ${token}`,
        Cookie: `FB_LB=${fbLb}`,
      },
      params,
      withCredentials: true,
    };

    const { data } = await axios(reqConfig);

    return data;
  } catch (err) {
    if (err.response && err.response.status !== 200) {
      logger.error(err.response.data.errors);
      throw new Error(err.response.data.detail);
    } else {
      logger.error(err);
      throw new Error(err);
    }
  }
};

export {
  getAccessToken,
  initiateStagingArea,
  uploadFile,
  getDefaultKeywordsGuid,
  postIndexingModifiers,
  archiveDocument,
  getDocumentById,
  getDocumentsByIds,
  getDocumentKeywords,
  getDocumentKeywordTypes,
  patchDocumentKeywords,
  getDocumentContent,
  getDocumentTypeByName,
  getKeywordTypesByNames,
  createQuery,
  getQueryResults,
};
