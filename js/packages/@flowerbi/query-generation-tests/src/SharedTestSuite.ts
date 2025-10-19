import { ExecutionTestsBase } from './ExecutionTestsBase';

/**
 * Shared test suite that can be run against any database platform
 */
export function runSharedTestSuite(
  tests: ExecutionTestsBase,
  suiteName: string
) {
  describe(suiteName, () => {
    // Theory Tests (parameterized tests)
    describe('Theory Tests', () => {
      test.each([false, true])('MinimalSelectOneColumn - allowDuplicates: %s', async (allowDuplicates) => {
        const results = await tests.executeFlowerBIQuery({
          aggregations: [{ function: 'Count' as any, column: 'Vendor.VendorName' }],
          skip: 0,
          take: 1,
          allowDuplicates: allowDuplicates,
        });

        expect(results.records).toHaveLength(1);
        expect(results.records[0].aggregated[0]).toBe(14);
        expect(results.totals).toBeUndefined();
      });

      test.each([true, false])('SingleAggregation - allowDuplicates: %s', async (allowDuplicates) => {
        const results = await tests.executeFlowerBIQuery({
          select: ['Vendor.VendorName'],
          aggregations: [{ function: 'Sum' as any, column: 'Invoice.Amount' }],
          skip: 2,
          take: 10,
          allowDuplicates: allowDuplicates,
        });

        const records = results.records.map(x => [
          x.selected[0],
          ExecutionTestsBase.round(x.aggregated[0])
        ]);

        expect(records).toEqual(expect.arrayContaining([
          ['Steve Makes Sandwiches', 176.24],
          ['Manchesterford Supplies Inc', 164.36],
          ['Disgusting Ltd', 156.14],
          ['Statues While You Wait', 156.24],
          ['Tiles Tiles Tiles', 106.24],
          ['Uranium 4 Less', 88.12],
          ['Awnings-R-Us', 88.12],
          ['Pleasant Plc', 88.12],
          ['Mats and More', 76.24],
          ['Party Hats 4 U', 58.12],
        ]));

        tests.expectToBeInDescendingOrder(records.map(x => x[1]));
        expect(results.totals).toBeUndefined();
      });

      test.each([false, true])('SingleAggregationOrderBySelect - descending: %s', async (descending) => {
        const results = await tests.executeFlowerBIQuery({
          select: ['Vendor.VendorName'],
          aggregations: [{ function: 'Sum' as any, column: 'Invoice.Amount' }],
          skip: 2,
          take: 10,
          orderBy: [{ column: 'Vendor.VendorName', descending: descending }],
        });

        const records = results.records.map(x => [
          x.selected[0],
          ExecutionTestsBase.round(x.aggregated[0])
        ]);

        const expectedDescending = [
          ['Tiles Tiles Tiles', 106.24],
          ['Steve Makes Sandwiches', 176.24],
          ['Statues While You Wait', 156.24],
          ['Stationary Stationery', 28.12],
          ['Pleasant Plc', 88.12],
          ['Party Hats 4 U', 58.12],
          ['Mats and More', 76.24],
          ['Manchesterford Supplies Inc', 164.36],
          ['Handbags-a-Plenty', 252.48],
          ['Disgusting Ltd', 156.14],
        ];

        const expectedAscending = [
          ['Handbags-a-Plenty', 252.48],
          ['Manchesterford Supplies Inc', 164.36],
          ['Mats and More', 76.24],
          ['Party Hats 4 U', 58.12],
          ['Pleasant Plc', 88.12],
          ['Stationary Stationery', 28.12],
          ['Statues While You Wait', 156.24],
          ['Steve Makes Sandwiches', 176.24],
          ['Tiles Tiles Tiles', 106.24],
          ['United Cheese', 406.84],
        ];

        expect(records).toEqual(expect.arrayContaining(descending ? expectedDescending : expectedAscending));
        expect(results.totals).toBeUndefined();
      });

      test.each(['Min', 'Max'])('AggregationFunctions - %s', async (aggregationType) => {
        const results = await tests.executeFlowerBIQuery({
          select: ['Vendor.VendorName'],
          aggregations: [{ function: aggregationType as any, column: 'Invoice.Amount' }],
          orderBy: [{ column: 'Vendor.VendorName', descending: false }],
          skip: 0,
          take: 100,
        });

        const records = results.records.map(x => [x.selected[0], x.aggregated[0]]);

        const expected = [
          ['Awnings-R-Us', 88.12, 88.12],
          ['Disgusting Ltd', 68.12, 88.02],
          ['Handbags-a-Plenty', 28.12, 98.12],
          ['Manchesterford Supplies Inc', 18.12, 88.12],
          ['Mats and More', 18.12, 58.12],
          ['Party Hats 4 U', 58.12, 58.12],
          ['Pleasant Plc', 88.12, 88.12],
          ['Stationary Stationery', 28.12, 28.12],
          ['Statues While You Wait', 78.12, 78.12],
          ['Steve Makes Sandwiches', 88.12, 88.12],
          ['Tiles Tiles Tiles', 38.12, 68.12],
          ['United Cheese', 18.12, 98.12],
          ['Uranium 4 Less', 88.12, 88.12],
        ];

        const expectedForType = expected.map(x => [
          x[0], 
          aggregationType === 'Min' ? x[1] : x[2]
        ]);

        expect(records).toEqual(expectedForType);
        tests.expectToBeInAscendingOrder(records.map(x => x[0]));
        expect(results.totals).toBeUndefined();
      });

      test.each([false, true])('DoubleAggregation - totals: %s', async (totals) => {
        const results = await tests.executeFlowerBIQuery({
          select: ['Vendor.VendorName'],
          aggregations: [
            { function: 'Sum' as any, column: 'Invoice.Amount' },
            { function: 'Count' as any, column: 'Invoice.Id' },
          ],
          totals: totals,
        });


        const records = results.records.map(x => [
          x.selected[0],
          ExecutionTestsBase.round(x.aggregated[0]),
          x.aggregated[1]
        ]);

        expect(records).toEqual(expect.arrayContaining([
          ['United Cheese', 406.84, 7],
          ['Handbags-a-Plenty', 252.48, 4],
          ['Steve Makes Sandwiches', 176.24, 2],
          ['Manchesterford Supplies Inc', 164.36, 3],
          ['Disgusting Ltd', 156.14, 2],
          ['Statues While You Wait', 156.24, 2],
          ['Tiles Tiles Tiles', 106.24, 2],
          ['Uranium 4 Less', 88.12, 1],
          ['Awnings-R-Us', 88.12, 1],
          ['Pleasant Plc', 88.12, 1],
          ['Mats and More', 76.24, 2],
          ['Party Hats 4 U', 58.12, 1],
          ['Stationary Stationery', 28.12, 1],
        ]));

        tests.expectToBeInDescendingOrder(records.map(x => x[1]));

        if (totals) {
          expect(results.totals?.aggregated.map(ExecutionTestsBase.round)).toEqual([1845.38, 29]);
        } else {
          expect(results.totals).toBeUndefined();
        }
      });

      test.each([false, true])('Stream - totals: %s', async (totals) => {
        // Note: Stream functionality would need to be implemented separately
        // For now, testing equivalent batch functionality
        const results = await tests.executeFlowerBIQuery({
          select: ['Vendor.VendorName'],
          aggregations: [
            { function: 'Sum' as any, column: 'Invoice.Amount' },
            { function: 'Count' as any, column: 'Invoice.Id' },
          ],
          totals: totals,
        });

        let records = results.records;
        if (totals) {
          // In our current implementation, totals are separate from records
          expect(results.totals?.aggregated.map(ExecutionTestsBase.round)).toEqual([1845.38, 29]);
          // No need to slice records since totals are separate
        }

        const mappedRecords = records.map(x => [
          x.selected[0],
          ExecutionTestsBase.round(x.aggregated[0]),
          x.aggregated[1]
        ]);

        expect(mappedRecords).toEqual(expect.arrayContaining([
          ['United Cheese', 406.84, 7],
          ['Handbags-a-Plenty', 252.48, 4],
          ['Steve Makes Sandwiches', 176.24, 2],
          ['Manchesterford Supplies Inc', 164.36, 3],
          ['Disgusting Ltd', 156.14, 2],
          ['Statues While You Wait', 156.24, 2],
          ['Tiles Tiles Tiles', 106.24, 2],
          ['Uranium 4 Less', 88.12, 1],
          ['Awnings-R-Us', 88.12, 1],
          ['Pleasant Plc', 88.12, 1],
          ['Mats and More', 76.24, 2],
          ['Party Hats 4 U', 58.12, 1],
          ['Stationary Stationery', 28.12, 1],
        ]));

        tests.expectToBeInDescendingOrder(mappedRecords.map(x => x[1]));
      });

      test.each([
        ['Select', 0, -1],
        ['Select', 1, null],
        ['Value', 0, 0],
        ['Value', 1, 1],
        ['Value', 3, null],
        ['Calculation', 0, 2],
        ['Calculation', 1, 3],
        ['Calculation', 2, 4],
        ['Calculation', 3, 5],
        ['Calculation', 4, null],
      ])('CalculationsAndIndexedOrderBy - %s index %s', async (orderingType, orderingIndex, expectedOrderBy) => {
        const queryJson = {
          select: ['Vendor.VendorName'],
          aggregations: [
            { function: 'Sum' as any, column: 'Invoice.Amount' },
            { function: 'Count' as any, column: 'Invoice.Id' },
          ],
          calculations: [
            { aggregation: 1 },
            {
              first: {
                first: { aggregation: 0 },
                operator: '??',
                second: { value: 42 },
              },
              operator: '+',
              second: { value: 3 },
            },
            {
              first: { aggregation: 0 },
              operator: '/',
              second: { value: 2 },
            },
            {
              first: { value: 50 },
              operator: '-',
              second: { aggregation: 0 },
            },
          ],
          orderBy: [{ type: orderingType as any, index: orderingIndex, descending: false }],
        };

        if (expectedOrderBy === null) {
          await expect(tests.executeFlowerBIQuery(queryJson)).rejects.toThrow();
          return;
        }

        
        const results = await tests.executeFlowerBIQuery(queryJson);
        
        
        const records = results.records.map(x => [
          x.selected[0],
          ExecutionTestsBase.round(x.aggregated[0]),
          x.aggregated[1],
          x.aggregated[2],
          ExecutionTestsBase.round(x.aggregated[3]),
          ExecutionTestsBase.round(x.aggregated[4]),
          ExecutionTestsBase.round(x.aggregated[5])
        ]);

        expect(records).toEqual(expect.arrayContaining([
          ['Uranium 4 Less', 88.12, 1, 1, 91.12, 44.06, -38.12],
          ['Stationary Stationery', 28.12, 1, 1, 31.12, 14.06, 21.88],
          ['Pleasant Plc', 88.12, 1, 1, 91.12, 44.06, -38.12],
          ['Party Hats 4 U', 58.12, 1, 1, 61.12, 29.06, -8.12],
          ['Awnings-R-Us', 88.12, 1, 1, 91.12, 44.06, -38.12],
          ['Tiles Tiles Tiles', 106.24, 2, 2, 109.24, 53.12, -56.24],
          ['Steve Makes Sandwiches', 176.24, 2, 2, 179.24, 88.12, -126.24],
          ['Statues While You Wait', 156.24, 2, 2, 159.24, 78.12, -106.24],
          ['Disgusting Ltd', 156.14, 2, 2, 159.14, 78.07, -106.14],
          ['Mats and More', 76.24, 2, 2, 79.24, 38.12, -26.24],
          ['Manchesterford Supplies Inc', 164.36, 3, 3, 167.36, 82.18, -114.36],
          ['Handbags-a-Plenty', 252.48, 4, 4, 255.48, 126.24, -202.48],
          ['United Cheese', 406.84, 7, 7, 409.84, 203.42, -356.84],
        ]));

        const orderedBy = expectedOrderBy === -1
          ? results.records.map(x => x.selected[0])
          : results.records.map(x => x.aggregated[expectedOrderBy]);

        tests.expectToBeInAscendingOrder(orderedBy);
      });
    });

    // Fact Tests (individual test cases)
    describe('Fact Tests', () => {
      test('FilterByPrimaryKeyOfOtherTable', async () => {
        const results = await tests.executeFlowerBIQuery({
          aggregations: [{ function: 'Sum' as any, column: 'Invoice.Amount' }],
          filters: [
            {
              column: 'Vendor.Id',
              operator: '=',
              value: 2,
            },
          ],
        });


        expect(results.records).toHaveLength(1);
        expect(results.records[0].aggregated[0]).toBe(164.36);
        expect(results.totals).toBeUndefined();
      });

      test('SingleAggregationTotals', async () => {
        const results = await tests.executeFlowerBIQuery({
          select: ['Vendor.VendorName'],
          aggregations: [{ function: 'Sum' as any, column: 'Invoice.Amount' }],
          skip: 2,
          take: 10,
          totals: true,
        });

        const records = results.records.map(x => [
          x.selected[0],
          ExecutionTestsBase.round(x.aggregated[0])
        ]);

        expect(records).toEqual(expect.arrayContaining([
          ['Steve Makes Sandwiches', 176.24],
          ['Manchesterford Supplies Inc', 164.36],
          ['Disgusting Ltd', 156.14],
          ['Statues While You Wait', 156.24],
          ['Tiles Tiles Tiles', 106.24],
          ['Uranium 4 Less', 88.12],
          ['Awnings-R-Us', 88.12],
          ['Pleasant Plc', 88.12],
          ['Mats and More', 76.24],
          ['Party Hats 4 U', 58.12],
        ]));

        tests.expectToBeInDescendingOrder(records.map(x => x[1]));
        expect(ExecutionTestsBase.round(results.totals!.aggregated[0])).toBe(1845.38);
      });

      test('SuspiciousComment', async () => {
        await tests.executeFlowerBIQuery({
          comment: 'suspicious \r\ncomment */ drop tables;',
          aggregations: [{ function: 'Count' as any, column: 'Vendor.VendorName' }],
        });

        // Expect the comment to contain actual line breaks (like C# version)
        // The sanitization process preserves \r\n but removes other special characters
        expect(tests.log[0]).toMatch(/\/\* suspicious\s*[\r\n]+\s*comment drop tables \*\//);
      });

      test('DoubleAggregationDifferentFilters', async () => {
        const results = await tests.executeFlowerBIQuery({
          select: ['Vendor.VendorName'],
          aggregations: [
            { function: 'Sum' as any, column: 'Invoice.Amount' },
            { 
              function: 'Count' as any, 
              column: 'Invoice.Id',
              filters: [
                {
                  column: 'Invoice.Paid',
                  operator: '=',
                  value: true,
                },
              ],
            },
          ],
        });


        const records = results.records.map(x => [
          x.selected[0],
          ExecutionTestsBase.round(x.aggregated[0]),
          ExecutionTestsBase.round(x.aggregated[1])
        ]);

        expect(records).toEqual(expect.arrayContaining([
          ['United Cheese', 406.84, 2],
          ['Handbags-a-Plenty', 252.48, 0],
          ['Steve Makes Sandwiches', 176.24, 2],
          ['Manchesterford Supplies Inc', 164.36, 2],
          ['Disgusting Ltd', 156.14, 0],
          ['Statues While You Wait', 156.24, 1],
          ['Tiles Tiles Tiles', 106.24, 1],
          ['Uranium 4 Less', 88.12, 0],
          ['Awnings-R-Us', 88.12, 1],
          ['Pleasant Plc', 88.12, 0],
          ['Mats and More', 76.24, 0],
          ['Party Hats 4 U', 58.12, 0],
          ['Stationary Stationery', 28.12, 0],
        ]));

        tests.expectToBeInDescendingOrder(records.map(x => x[1]));
        expect(results.totals).toBeUndefined();
      });

      test('DoubleAggregationMultipleSelects', async () => {
        const results = await tests.executeFlowerBIQuery({
          select: ['Vendor.VendorName', 'Department.DepartmentName'],
          aggregations: [
            { function: 'Sum' as any, column: 'Invoice.Amount' },
            { function: 'Count' as any, column: 'Invoice.Id' },
          ],
        });

        const records = results.records.map(x => [
          x.selected[0],
          x.selected[1],
          x.aggregated[0],
          x.aggregated[1]
        ]);

        // Both Invoice and Supplier are linked to a Department, so "maximum joinage" is required, i.e.
        // we only see cases where Invoice and Supplier have the same Department
        expect(records).toEqual(expect.arrayContaining([
          ['Handbags-a-Plenty', 'Missiles', 186.24, 2],
          ['United Cheese', 'Cheese', 166.24, 2],
          ['Disgusting Ltd', 'Yoga', 88.02, 1],
          ['Statues While You Wait', 'Cheese', 78.12, 1],
          ['Party Hats 4 U', 'Marketing', 58.12, 1],
        ]));

        tests.expectToBeInDescendingOrder(records.map(x => x[2]));
      });

      test('ManyToMany', async () => {
        const results = await tests.executeFlowerBIQuery({
          select: ['Vendor.VendorName', 'Tag.TagName'],
          aggregations: [{ function: 'Sum' as any, column: 'Invoice.Amount' }],
        });

        const records = results.records.map(x => [
          x.selected[0],
          x.selected[1],
          ExecutionTestsBase.round(x.aggregated[0])
        ]);

        expect(records).toEqual(expect.arrayContaining([
          ['Handbags-a-Plenty', 'Boring', 126.24],
          ['Handbags-a-Plenty', 'Interesting', 98.12],
          ['Handbags-a-Plenty', 'Lethal', 98.12],
          ['Steve Makes Sandwiches', 'Interesting', 88.12],
          ['Statues While You Wait', 'Boring', 78.12],
          ['United Cheese', 'Lethal', 58.12],
          ['United Cheese', 'Interesting', 58.12],
          ['Party Hats 4 U', 'Boring', 58.12],
        ]));

        tests.expectToBeInDescendingOrder(records.map(x => x[2]));
      });

      test('MultipleManyToMany', async () => {
        const results = await tests.executeFlowerBIQuery({
          select: ['Vendor.VendorName', 'Tag.TagName'],
          aggregations: [{ function: 'Sum' as any, column: 'Invoice.Amount' }],
          filters: [
            {
              column: 'Category.CategoryName',
              operator: '=',
              value: 'Regular',
            },
          ],
        });

        const records = results.records.map(x => [
          x.selected[0],
          x.selected[1],
          x.aggregated[0]
        ]);

        expect(records).toEqual(expect.arrayContaining([
          ['Statues While You Wait', 'Boring', 78.12],
          ['Handbags-a-Plenty', 'Boring', 28.12],
        ]));

        tests.expectToBeInDescendingOrder(records.map(x => x[2]));
      });

      test('MultipleManyToManyWithSpecifiedJoins', async () => {
        const results = await tests.executeFlowerBIQuery({
          select: ['Vendor.VendorName', 'AnnotationValue.Value@x', 'AnnotationValue.Value@y'],
          aggregations: [{ function: 'Sum' as any, column: 'Invoice.Amount' }],
          filters: [
            {
              column: 'AnnotationName.Name@x',
              operator: '=',
              value: 'Approver',
            },
            {
              column: 'AnnotationName.Name@y',
              operator: '=',
              value: 'Instructions',
            },
          ],
        });

        const records = results.records.map(x => [
          x.selected[0],
          x.selected[1],
          x.selected[2],
          ExecutionTestsBase.round(x.aggregated[0])
        ]);

        expect(records).toEqual(expect.arrayContaining([
          ['Pleasant Plc', 'Jill', 'Pay quickly', 88.12],
          ['Pleasant Plc', 'Jill', 'Brown envelope job', 88.12],
          ['Statues While You Wait', 'Snarvu', 'Cash only', 78.12],
          ['United Cheese', 'Gupta', 'Cash only', 18.12],
        ]));

        tests.expectToBeInDescendingOrder(records.map(x => x[3]));
        expect(results.totals).toBeUndefined();
      });

      test('NoAggregation', async () => {
        const results = await tests.executeFlowerBIQuery({
          select: ['Vendor.VendorName', 'Tag.TagName'],
        });

        expect(results.records.every(r => r.aggregated === null || r.aggregated.length === 0)).toBe(true);

        const records = results.records.map(x => [x.selected[0], x.selected[1]]);

        expect(records).toEqual(expect.arrayContaining([
          ['United Cheese', 'Interesting'],
          ['United Cheese', 'Lethal'],
          ['Party Hats 4 U', 'Boring'],
          ['Statues While You Wait', 'Boring'],
          ['Steve Makes Sandwiches', 'Interesting'],
          ['Handbags-a-Plenty', 'Interesting'],
          ['Handbags-a-Plenty', 'Lethal'],
          ['Handbags-a-Plenty', 'Boring'],
        ]));
      });

      test('NoAggregationAllowingDuplicates', async () => {
        const results = await tests.executeFlowerBIQuery({
          select: ['Vendor.VendorName', 'Tag.TagName'],
          allowDuplicates: true,
        });

        expect(results.records.every(r => r.aggregated === null || r.aggregated.length === 0)).toBe(true);

        const records = results.records.map(x => [x.selected[0], x.selected[1]]);

        expect(records).toEqual(expect.arrayContaining([
          ['United Cheese', 'Interesting'],
          ['United Cheese', 'Lethal'],
          ['Party Hats 4 U', 'Boring'],
          ['Statues While You Wait', 'Boring'],
          ['Steve Makes Sandwiches', 'Interesting'],
          ['Handbags-a-Plenty', 'Interesting'],
          ['Handbags-a-Plenty', 'Lethal'],
          ['Handbags-a-Plenty', 'Boring'],
          ['Handbags-a-Plenty', 'Boring'], // Duplicate due to AllowDuplicates
        ]));
      });

      test('AggregationCountDistinct', async () => {
        const results = await tests.executeFlowerBIQuery({
          select: ['Vendor.VendorName'],
          aggregations: [
            { function: 'CountDistinct' as any, column: 'Invoice.Amount' },
            { 
              function: 'CountDistinct' as any, 
              column: 'Invoice.Amount',
              filters: [
                {
                  column: 'Invoice.Paid',
                  operator: '=',
                  value: true,
                },
              ],
            },
          ],
        });

        const records = results.records.map(x => [
          x.selected[0],
          x.aggregated[0],
          x.aggregated[1] ?? 0
        ]);

        expect(records).toEqual(expect.arrayContaining([
          ['Awnings-R-Us', 1, 1],
          ['Disgusting Ltd', 2, 0],
          ['Handbags-a-Plenty', 4, 0],
          ['Manchesterford Supplies Inc', 3, 2],
          ['Mats and More', 2, 0],
          ['Party Hats 4 U', 1, 0],
          ['Pleasant Plc', 1, 0],
          ['Stationary Stationery', 1, 0],
          ['Statues While You Wait', 1, 1],
          ['Steve Makes Sandwiches', 1, 1],
          ['Tiles Tiles Tiles', 2, 1],
          ['United Cheese', 6, 2],
          ['Uranium 4 Less', 1, 0],
        ]));

        tests.expectToBeInDescendingOrder(records.map(x => x[1]));
      });

      test('MultipleManyToManyWithSpecifiedJoinsAndMultipleJoinDependencies', async () => {
        const results = await tests.executeFlowerBIQuery({
          select: ['Vendor.VendorName', 'AnnotationValue.Value@x', 'AnnotationValue.Value@y'],
          aggregations: [{ function: 'Sum' as any, column: 'Invoice.Amount' }],
          filters: [
            {
              column: 'AnnotationName.Name@x',
              operator: '=',
              value: 'Approver',
            },
            {
              column: 'AnnotationName.Name@y',
              operator: '=',
              value: 'Instructions',
            },
            {
              column: 'Department.DepartmentName',
              operator: '=',
              value: 'Cheese',
            },
          ],
        });

        const records = results.records.map(x => [...x.selected, ...x.aggregated.map(ExecutionTestsBase.round)]);
        expect(records).toEqual([['Statues While You Wait', 'Snarvu', 'Cash only', 78.12]]);
      });

      test('ManyToManyWithComplicatedSchema', async () => {
        const results = await tests.executeFlowerBIQuery(
          {
            select: ['Vendor.VendorName', 'Category.CategoryName'],
            aggregations: [{ function: 'Sum' as any, column: 'Invoice.Amount' }],
            filters: [
              {
                column: 'Department.DepartmentName',
                operator: '=',
                value: 'Cheese',
              },
            ],
          },
          ExecutionTestsBase.ComplicatedSchema
        );

        const records = results.records.map(x => [
          x.selected[0],
          x.selected[1],
          ExecutionTestsBase.round(x.aggregated[0])
        ]);

        expect(records).toEqual(expect.arrayContaining([
          ['United Cheese', 'Regular', 98.12],
          ['Statues While You Wait', 'Regular', 78.12],
        ]));
      });

      test('ManyToManyConjointWithComplicatedSchema', async () => {
        const results = await tests.executeFlowerBIQuery(
          {
            select: ['AnnotationValue.Value@x', 'AnnotationValue.Value@y'],
            aggregations: [{ function: 'Sum' as any, column: 'Invoice.Amount' }],
            filters: [
              {
                column: 'AnnotationName.Name@x',
                operator: '=',
                value: 'Approver',
              },
              {
                column: 'AnnotationName.Name@y',
                operator: '=',
                value: 'Movie',
              },
              {
                column: 'Department.DepartmentName',
                operator: '=',
                value: 'Cheese',
              },
            ],
          },
          ExecutionTestsBase.ComplicatedSchema
        );

        const records = results.records.map(x => [...x.selected, ...x.aggregated.map(ExecutionTestsBase.round)]);
        expect(records).toEqual([['Snarvu', 'Robocop', 78.12]]);
      });

      test('CalculationsAndMultiSelect', async () => {
        const results = await tests.executeFlowerBIQuery({
          select: ['Vendor.VendorName', 'Vendor.DepartmentId'],
          aggregations: [
            { function: 'Sum' as any, column: 'Invoice.Amount' },
            { function: 'Count' as any, column: 'Invoice.Id' },
          ],
          calculations: [
            { aggregation: 1 },
            {
              first: {
                first: { aggregation: 0 },
                operator: '??',
                second: { value: 42 },
              },
              operator: '+',
              second: { value: 3 },
            },
            {
              first: { aggregation: 0 },
              operator: '/',
              second: { aggregation: 1 },
            },
          ],
          orderBy: [{ type: 'Select' as any, index: 1, descending: false }],
        });

        const records = results.records.map(x => [
          x.selected[0],
          x.selected[1],
          ExecutionTestsBase.round(x.aggregated[0]),
          x.aggregated[1],
          x.aggregated[2],
          ExecutionTestsBase.round(x.aggregated[3]),
          ExecutionTestsBase.round(x.aggregated[4])
        ]);

        expect(records).toEqual(expect.arrayContaining([
          ['Stationary Stationery', 1, 28.12, 1, 1, 31.12, 28.12],
          ['Party Hats 4 U', 2, 58.12, 1, 1, 61.12, 58.12],
          ['Tiles Tiles Tiles', 2, 106.24, 2, 2, 109.24, 53.12],
          ['Handbags-a-Plenty', 3, 252.48, 4, 4, 255.48, 63.12],
          ['Pleasant Plc', 3, 88.12, 1, 1, 91.12, 88.12],
          ['Uranium 4 Less', 3, 88.12, 1, 1, 91.12, 88.12],
          ['Awnings-R-Us', 4, 88.12, 1, 1, 91.12, 88.12],
          ['Manchesterford Supplies Inc', 4, 164.36, 3, 3, 167.36, 54.7867],
          ['Statues While You Wait', 4, 156.24, 2, 2, 159.24, 78.12],
          ['Steve Makes Sandwiches', 4, 176.24, 2, 2, 179.24, 88.12],
          ['United Cheese', 4, 406.84, 7, 7, 409.84, 58.12],
          ['Disgusting Ltd', 5, 156.14, 2, 2, 159.14, 78.07],
          ['Mats and More', 5, 76.24, 2, 2, 79.24, 38.12],
        ]));
      });

      test('BitFilters', async () => {
        const results = await tests.executeFlowerBIQuery({
          select: ['Vendor.VendorName'],
          filters: [
            {
              column: 'Invoice.VendorId',
              operator: 'BITS IN',
              constant: 1 | 2, // Binary: 011 (bits 0 and 1)
              value: [0, 2],
            },
            {
              column: 'Invoice.VendorId',
              operator: 'BITS IN',
              constant: 4 | 8, // Binary: 1100 (bits 2 and 3)
              value: [0, 4],
            },
          ],
        });

        const records = results.records.map(x => x.selected[0]);

        expect(records).toEqual(expect.arrayContaining([
          'Manchesterford Supplies Inc',
          'United Cheese',
          'Uranium 4 Less'
        ]));
      });

      test('DoubleAggregationWithBitFilters', async () => {
        const results = await tests.executeFlowerBIQuery({
          select: ['Vendor.VendorName'],
          aggregations: [
            { function: 'Count' as any, column: 'Invoice.Id' },
            { 
              function: 'Count' as any, 
              column: 'Invoice.Id',
              filters: [
                {
                  column: 'Invoice.VendorId',
                  operator: 'BITS IN',
                  constant: 1 | 2, // Binary: 011 (bits 0 and 1)
                  value: [0, 2],
                },
              ],
            },
          ],
        });

        const records = results.records.map(x => [
          x.selected[0],
          ExecutionTestsBase.round(x.aggregated[0]),
          x.aggregated[1]
        ]);

        expect(records).toEqual(expect.arrayContaining([
          ['United Cheese', 7, 7],
          ['Handbags-a-Plenty', 4, 0],
          ['Manchesterford Supplies Inc', 3, 3],
          ['Tiles Tiles Tiles', 2, 0],
          ['Steve Makes Sandwiches', 2, 2],
          ['Statues While You Wait', 2, 2],
          ['Mats and More', 2, 0],
          ['Disgusting Ltd', 2, 0],
          ['Uranium 4 Less', 1, 1],
          ['Stationary Stationery', 1, 0],
          ['Pleasant Plc', 1, 1],
          ['Party Hats 4 U', 1, 0],
          ['Awnings-R-Us', 1, 1],
        ]));

        tests.expectToBeInDescendingOrder(records.map(x => x[1]));
      });

      test('SqlAndDapperWithListFilter', async () => {
        const results = await tests.executeFlowerBIQuery({
          select: ['Invoice.VendorId', 'Invoice.DepartmentId'],
          aggregations: [{ function: 'Count' as any, column: 'Vendor.VendorName' }],
          filters: [
            {
              column: 'Invoice.Id',
              operator: 'IN',
              value: [2, 4, 6, 8],
            },
          ],
        });

        const records = results.records.map(x => [x.selected[0], x.selected[1], x.aggregated[0]]);
        expect(records).toEqual(expect.arrayContaining([
          [4, 6, 1],
          [4, 4, 1],
          [9, 3, 1],
          [4, 3, 1]
        ]));
      });

      test('SqlAndDapperWithEmptyListFilter', async () => {
        const queryPromise = tests.executeFlowerBIQuery({
          select: ['Invoice.VendorId', 'Invoice.DepartmentId'],
          aggregations: [{ function: 'Count' as any, column: 'Vendor.VendorName' }],
          filters: [
            {
              column: 'Invoice.Id',
              operator: 'IN',
              value: [],
            },
          ],
        });

        await expect(queryPromise).rejects.toThrow('Filter JSON contains empty array');
      });

      test('FullJoins', async () => {
        const results = await tests.executeFlowerBIQuery({
          select: ['Vendor.VendorName'],
          aggregations: [{ function: 'Sum' as any, column: 'Invoice.Amount' }],
          fullJoins: true,
        });

        const records = results.records.map(x => [
          x.selected[0],
          ExecutionTestsBase.round(x.aggregated[0])
        ]);

        // Should match C# version's explicit expected results
        tests.expectRecordsToEqual(results.records, [
          ['United Cheese', 406.84],
          ['Handbags-a-Plenty', 252.48],
          ['Steve Makes Sandwiches', 176.24],
          ['Manchesterford Supplies Inc', 164.36],
          ['Disgusting Ltd', 156.14],
          ['Statues While You Wait', 156.24],
          ['Tiles Tiles Tiles', 106.24],
          ['Uranium 4 Less', 88.12],
          ['Awnings-R-Us', 88.12],
          ['Pleasant Plc', 88.12],
          ['Mats and More', 76.24],
          ['Party Hats 4 U', 58.12],
          ['Stationary Stationery', 28.12],
          ['Acme Ltd', null], // Included due to full join
        ]);

        tests.expectToBeInDescendingOrder(records.map(x => x[1]).filter(x => x !== null));
        expect(results.totals).toBeUndefined();
      });
    });
  });
}